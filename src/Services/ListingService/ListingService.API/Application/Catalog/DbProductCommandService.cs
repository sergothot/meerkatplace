using Common.Shared.Application.Interfaces;
using Common.Shared.Domain.Enums;
using ListingService.API.Application.Abstractions;
using ListingService.API.Domain.Entities;
using ListingService.API.Domain.Enums;

namespace ListingService.API.Application.Catalog;

public sealed class DbProductCommandService(
    IProductRepository products,
    IInventoryStockRepository stocks,
    IUnitOfWork unitOfWork) : IProductCommandService
{
    public async Task<IResult> CreateProductAsync(CreateProductRequest request)
    {
        var errors = CatalogRequestValidator.ValidateCreateProduct(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        if (!TryParseCurrency(request.Currency, out var currency) ||
            !TryParseDeliveryType(request.DeliveryType, out var deliveryType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["payload"] = ["Invalid currency or delivery type."]
            });
        }

        Product product;
        try
        {
            product = Product.Create(
                request.SellerId,
                request.Name,
                request.Description,
                request.Price,
                currency,
                deliveryType);
            product.Activate();
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_PRODUCT",
                    message = ex.Message
                }
            });
        }

        await products.AddAsync(product);

        if (request.StockQuantity.HasValue)
        {
            await stocks.AddAsync(new InventoryStock
            {
                ProductId = product.Id,
                Quantity = request.StockQuantity.Value,
                Reserved = 0
            });
        }

        await unitOfWork.SaveChangesAsync();

        return Results.Created($"/products/{product.Id}", new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Currency.ToString(),
            product.DeliveryType.ToString()));
    }

    public async Task<IResult> UpdateProductAsync(Guid productId, UpdateProductRequest request)
    {
        var errors = CatalogRequestValidator.ValidateUpdateProduct(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var product = await products.GetByIdAsync(productId);
        if (product is null)
            return Results.NotFound();

        var updatedName = string.IsNullOrWhiteSpace(request.Name) ? product.Name : request.Name;
        var updatedDescription = string.IsNullOrWhiteSpace(request.Description) ? product.Description : request.Description;
        var updatedPrice = request.Price ?? product.Price;

        try
        {
            product.UpdateDetails(updatedName, updatedDescription, updatedPrice);

            if (!string.IsNullOrWhiteSpace(request.Status) && TryParseStatus(request.Status, out var status))
            {
                ApplyStatus(product, status);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_PRODUCT_UPDATE",
                    message = ex.Message
                }
            });
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync();

        return Results.Ok(new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Currency.ToString(),
            product.DeliveryType.ToString()));
    }

    public async Task<IResult> UpdateStockAsync(Guid productId, UpdateStockRequest request)
    {
        var errors = CatalogRequestValidator.ValidateUpdateStock(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var product = await products.GetByIdAsync(productId);
        if (product is null)
            return Results.NotFound();

        var stock = await stocks.GetByProductIdAsync(productId);
        if (stock is null)
        {
            stock = new InventoryStock
            {
                ProductId = productId,
                Quantity = request.Quantity,
                Reserved = request.Reserved
            };
            await stocks.AddAsync(stock);
        }
        else
        {
            stock.Quantity = request.Quantity;
            stock.Reserved = 0;
            stock.TryReserve(request.Reserved);
            stocks.Update(stock);
        }

        await unitOfWork.SaveChangesAsync();
        return Results.Ok(new { productId, quantity = stock.Quantity, reserved = stock.Reserved });
    }

    public async Task<IResult> RemoveProductAsync(Guid productId)
    {
        var product = await products.GetByIdAsync(productId);
        if (product is null)
            return Results.NotFound();

        product.Deactivate();
        products.Update(product);
        await unitOfWork.SaveChangesAsync();

        return Results.NoContent();
    }

    private static bool TryParseCurrency(string value, out Currency currency)
    {
        return Enum.TryParse(value, true, out currency);
    }

    private static bool TryParseDeliveryType(string value, out DeliveryType deliveryType)
    {
        return Enum.TryParse(value, true, out deliveryType);
    }

    private static bool TryParseStatus(string value, out ProductStatus status)
    {
        return Enum.TryParse(value, true, out status);
    }

    private static void ApplyStatus(Product product, ProductStatus status)
    {
        switch (status)
        {
            case ProductStatus.Active:
                product.Activate();
                break;
            case ProductStatus.Inactive:
                product.Deactivate();
                break;
            case ProductStatus.Archived:
                product.Archive();
                break;
            case ProductStatus.Draft:
                product.Deactivate();
                break;
        }
    }
}
