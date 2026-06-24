using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Marketplace.IntegrationTests;

public class CheckoutFlowIntegrationTests
{
    private static readonly HttpClient GatewayClient = CreateClient("http://localhost:8080");
    private static readonly HttpClient UserClient = CreateClient("http://localhost:5001");
    private static readonly HttpClient ListingClient = CreateClient("http://localhost:5002");
    private static readonly HttpClient OrderClient = CreateClient("http://localhost:5003");
    private static readonly HttpClient PaymentClient = CreateClient("http://localhost:5004");

    [Fact]
    public async Task HealthEndpoints_ReturnOkWithServiceName()
    {
        await AssertHealthAsync(GatewayClient, "api-gateway");
        await AssertHealthAsync(UserClient, "user-service");
        await AssertHealthAsync(ListingClient, "listing-service");
        await AssertHealthAsync(OrderClient, "order-service");
        await AssertHealthAsync(PaymentClient, "payment-service");
    }

    [Fact]
    public async Task Checkout_HappyPath_EventuallyPaid_AndReturnsShipment()
    {
        var buyerId = Guid.NewGuid();

        var productId = await CreateProductAsync(
            name: $"Integration Happy {DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            stock: 5,
            price: 500m);

        await AddCartItemAsync(productId, quantity: 2, unitPrice: 500m, buyerId);

        var checkout = await CheckoutAsync(buyerId);
        Assert.Equal("Placed", checkout.Status);
        Assert.True(checkout.RequiresPayment);
        Assert.True(checkout.Amount > 0);

        var finalStatus = await WaitForOrderStatusAsync(checkout.OrderId, expectedStatus: "Paid", maxAttempts: 15, buyerId);
        Assert.Equal("Paid", finalStatus);

        var shipmentsRequest = new HttpRequestMessage(HttpMethod.Get, $"/orders/{checkout.OrderId}/shipments");
        shipmentsRequest.Headers.Add("X-User-Id", buyerId.ToString());

        var shipmentsResponse = await OrderClient.SendAsync(shipmentsRequest);
        shipmentsResponse.EnsureSuccessStatusCode();

        var shipmentsJson = await shipmentsResponse.Content.ReadAsStringAsync();
        using var shipmentsDoc = JsonDocument.Parse(shipmentsJson);

        Assert.Equal(JsonValueKind.Array, shipmentsDoc.RootElement.ValueKind);
        Assert.True(shipmentsDoc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Checkout_InsufficientStock_EventuallyCancelled()
    {
        var buyerId = Guid.NewGuid();

        var productId = await CreateProductAsync(
            name: $"Integration Fail {DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            stock: 1,
            price: 200m);

        await AddCartItemAsync(productId, quantity: 3, unitPrice: 200m, buyerId);

        var checkout = await CheckoutAsync(buyerId);
        Assert.Equal("Placed", checkout.Status);

        var finalStatus = await WaitForOrderStatusAsync(checkout.OrderId, expectedStatus: "Cancelled", maxAttempts: 15, buyerId);
        Assert.Equal("Cancelled", finalStatus);
    }

    private static async Task AssertHealthAsync(HttpClient client, string expectedService)
    {
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
        Assert.NotNull(payload);
        Assert.Equal("ok", payload!.Status);
        Assert.Equal(expectedService, payload.Service);
    }

    private static async Task<Guid> CreateProductAsync(string name, int stock, decimal price)
    {
        var body = new
        {
            sellerId = "11111111-1111-1111-1111-111111111111",
            name,
            description = "integration",
            price,
            currency = "RUB",
            deliveryType = "Physical",
            stockQuantity = stock
        };

        var response = await ListingClient.PostAsJsonAsync("/products", body);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.Id);

        return payload.Id;
    }

    private static async Task AddCartItemAsync(Guid productId, int quantity, decimal unitPrice, Guid buyerId)
    {
        var body = new
        {
            productId,
            quantity,
            unitPrice,
            currency = "RUB"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/cart/items")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-User-Id", buyerId.ToString());

        var response = await OrderClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<CheckoutResponse> CheckoutAsync(Guid buyerId)
    {
        var body = new
        {
            addressId = "demo-address",
            paymentMethod = "Wallet"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/cart/checkout")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-User-Id", buyerId.ToString());

        var response = await OrderClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.OrderId);

        return payload;
    }

    private static async Task<string> WaitForOrderStatusAsync(Guid orderId, string expectedStatus, int maxAttempts, Guid buyerId)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/orders/{orderId}/status");
            request.Headers.Add("X-User-Id", buyerId.ToString());

            var response = await OrderClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<OrderStatusResponse>();
            Assert.NotNull(payload);

            if (string.Equals(payload!.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return payload.Status;
            }

            await Task.Delay(1000);
        }

        return "TimedOut";
    }

    private static HttpClient CreateClient(string baseAddress)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private sealed record HealthPayload(string Status, string Service);
    private sealed record ProductResponse(Guid Id);
    private sealed record CheckoutResponse(Guid OrderId, string Status, decimal Amount, string Currency, bool RequiresPayment);
    private sealed record OrderStatusResponse(Guid OrderId, string Status);
}

public class GatewayContractIntegrationTests
{
    private static readonly HttpClient GatewayClient = new()
    {
        BaseAddress = new Uri("http://localhost:8080"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    [Fact]
    public async Task Auth_RegisterAndLogin_ReturnsExpectedContract()
    {
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid().ToString("N")[..8];
        var login = $"itest_{stamp}_{nonce}";
        var email = $"itest_{stamp}_{nonce}@example.com";
        const string password = "Password123!";

        var registerResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
                {
                    Content = JsonContent.Create(new
                    {
                        login,
                        email,
                        password
                    })
                };

                return request;
            },
            maxAttempts: 4);
        Assert.True(
            registerResponse.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict or HttpStatusCode.InternalServerError,
            $"Unexpected register status code: {registerResponse.StatusCode}");

        var loginResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
                {
                    Content = JsonContent.Create(new
                    {
                        email,
                        password
                    })
                };

                return request;
            },
            maxAttempts: 6);
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginJson);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("accessToken", out var accessToken));
        Assert.False(string.IsNullOrWhiteSpace(accessToken.GetString()));

        Assert.True(root.TryGetProperty("refreshToken", out var refreshToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.GetString()));

        Assert.True(root.TryGetProperty("expiresIn", out var expiresIn));
        Assert.True(expiresIn.GetInt32() > 0);

        Assert.True(root.TryGetProperty("user", out var user));
        Assert.True(user.TryGetProperty("id", out _));
        Assert.True(user.TryGetProperty("login", out var userLogin));
        Assert.Equal(login, userLogin.GetString());
        Assert.True(user.TryGetProperty("email", out var userEmail));
        Assert.Equal(email, userEmail.GetString());
        Assert.True(user.TryGetProperty("roles", out _));
    }

    [Fact]
    public async Task ProtectedOrderRoute_WithoutToken_ReturnsUnauthorized()
    {
        var response = await GatewayClient.GetAsync("/api/v1/order/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListingProducts_ContractContainsExpectedFields()
    {
        var name = $"Contract Product {DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var createResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/listing/products")
                {
                    Content = JsonContent.Create(new
                    {
                        sellerId = "11111111-1111-1111-1111-111111111111",
                        name,
                        description = "contract",
                        price = 123.45m,
                        currency = "RUB",
                        deliveryType = "Physical",
                        stockQuantity = 2
                    })
                };

                return request;
            },
            maxAttempts: 4);
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await SendGatewayWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/v1/listing/products?q={Uri.EscapeDataString(name)}"),
            maxAttempts: 4);
        listResponse.EnsureSuccessStatusCode();

        var listJson = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listJson);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);

        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("description", out _));
        Assert.True(first.TryGetProperty("price", out _));
        Assert.True(first.TryGetProperty("currency", out _));
        Assert.True(first.TryGetProperty("deliveryType", out _));
    }

    private static async Task<HttpResponseMessage> SendGatewayWithRetryAsync(Func<HttpRequestMessage> requestFactory, int maxAttempts)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();

            try
            {
                var response = await GatewayClient.SendAsync(request);
                if (IsTransientStatusCode(response.StatusCode) && attempt < maxAttempts)
                {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw new InvalidOperationException("Gateway request failed after retries.", lastException);
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is TaskCanceledException or HttpRequestException;
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests
            || statusCode == HttpStatusCode.InternalServerError
            || statusCode == HttpStatusCode.BadGateway
            || statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.GatewayTimeout;
    }
}

public class AuthenticatedGatewayContractIntegrationTests
{
    private static readonly HttpClient GatewayClient = new()
    {
        BaseAddress = new Uri("http://localhost:8080"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    [Fact]
    public async Task UserProfile_WithBearerToken_ReturnsExpectedContract()
    {
        var auth = await RegisterAndLoginAsync();

        var response = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user/users/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return request;
            },
            maxAttempts: 4);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.Equal(auth.UserId.ToString(), root.GetProperty("id").GetString());
        Assert.Equal(auth.Login, root.GetProperty("login").GetString());
        Assert.Equal(auth.Email, root.GetProperty("email").GetString());
        Assert.True(root.TryGetProperty("roles", out _));
        Assert.True(root.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task UserAddress_CreateThenList_ReturnsCreatedAddress()
    {
        var auth = await RegisterAndLoginAsync();

        var createResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/user/users/me/addresses")
                {
                    Content = JsonContent.Create(new
                    {
                        country = "RU",
                        city = "Moscow",
                        street = "Tverskaya",
                        postalCode = "101000"
                    })
                };
                createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return createRequest;
            },
            maxAttempts: 4);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user/users/me/addresses");
                listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return listRequest;
            },
            maxAttempts: 4);
        listResponse.EnsureSuccessStatusCode();

        var listJson = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listJson);
        Assert.Equal(JsonValueKind.Array, listDoc.RootElement.ValueKind);
        Assert.True(listDoc.RootElement.GetArrayLength() >= 1);

        var first = listDoc.RootElement[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.Equal("RU", first.GetProperty("country").GetString());
        Assert.Equal("Moscow", first.GetProperty("city").GetString());
        Assert.Equal("Tverskaya", first.GetProperty("street").GetString());
        Assert.Equal("101000", first.GetProperty("postalCode").GetString());
    }

    [Fact]
    public async Task OrderStatus_WithAuthAndUserScope_ReturnsExpectedShape()
    {
        var auth = await RegisterAndLoginAsync();
        var orderId = await CreateCheckoutOrderViaGatewayAsync(auth.AccessToken, auth.UserId, 150m);

        var status = await WaitForOrderStatusViaGatewayAsync(orderId, auth.AccessToken, auth.UserId, 15);
        Assert.True(status is "Placed" or "Paid" or "Cancelled");
    }

    [Fact]
    public async Task OrderDetails_WithAuth_ReturnsExpectedContract()
    {
        var auth = await RegisterAndLoginAsync();
        var orderId = await CreateCheckoutOrderViaGatewayAsync(auth.AccessToken, auth.UserId, 175m);

        var detailsResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var detailsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/order/orders/{orderId}");
                detailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                detailsRequest.Headers.Add("X-User-Id", auth.UserId.ToString());
                return detailsRequest;
            },
            maxAttempts: 4);
        detailsResponse.EnsureSuccessStatusCode();

        var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
        using var detailsDoc = JsonDocument.Parse(detailsJson);

        var root = detailsDoc.RootElement;
        Assert.Equal(orderId, root.GetProperty("orderId").GetGuid());
        Assert.Equal(auth.UserId, root.GetProperty("buyerId").GetGuid());
        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("currency", out _));
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Payment_CreateGetRefund_WithAuth_ReturnsExpectedContract()
    {
        var auth = await RegisterAndLoginAsync();
        var orderId = Guid.NewGuid();

        var createResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment/payments")
                {
                    Content = JsonContent.Create(new
                    {
                        orderId,
                        method = "Wallet",
                        amount = 199.99m,
                        currency = "RUB"
                    })
                };
                createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return createRequest;
            },
            maxAttempts: 4);
        createResponse.EnsureSuccessStatusCode();

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var paymentId = createDoc.RootElement.GetProperty("paymentId").GetGuid();

        Assert.Equal(orderId, createDoc.RootElement.GetProperty("orderId").GetGuid());
        Assert.True(createDoc.RootElement.TryGetProperty("status", out _));
        Assert.True(createDoc.RootElement.TryGetProperty("amount", out _));
        Assert.True(createDoc.RootElement.TryGetProperty("currency", out _));
        Assert.True(createDoc.RootElement.TryGetProperty("method", out _));
        Assert.True(createDoc.RootElement.TryGetProperty("createdAt", out _));

        var getResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payment/payments/{paymentId}");
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return getRequest;
            },
            maxAttempts: 4);
        getResponse.EnsureSuccessStatusCode();

        var getJson = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getJson);
        Assert.Equal(paymentId, getDoc.RootElement.GetProperty("paymentId").GetGuid());

        var refundResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var refundRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payment/payments/{paymentId}/refund")
                {
                    Content = JsonContent.Create(new { })
                };
                refundRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                return refundRequest;
            },
            maxAttempts: 4);
        refundResponse.EnsureSuccessStatusCode();

        var refundJson = await refundResponse.Content.ReadAsStringAsync();
        using var refundDoc = JsonDocument.Parse(refundJson);
        Assert.Equal("Refunded", refundDoc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ProtectedPaymentRoute_WithoutToken_ReturnsUnauthorized()
    {
        var response = await GatewayClient.GetAsync($"/api/v1/payment/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> WaitForOrderStatusViaGatewayAsync(Guid orderId, string accessToken, Guid userId, int maxAttempts)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var statusResponse = await SendGatewayWithRetryAsync(
                () =>
                {
                    var statusRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/order/orders/{orderId}/status");
                    statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    statusRequest.Headers.Add("X-User-Id", userId.ToString());
                    return statusRequest;
                },
                maxAttempts: 3);

            if (!statusResponse.IsSuccessStatusCode)
            {
                await Task.Delay(1000);
                continue;
            }

            var statusJson = await statusResponse.Content.ReadAsStringAsync();
            using var statusDoc = JsonDocument.Parse(statusJson);
            var status = statusDoc.RootElement.GetProperty("status").GetString() ?? string.Empty;

            if (status is "Paid" or "Cancelled")
            {
                return status;
            }

            await Task.Delay(1000);
        }

        return "TimedOut";
    }

    private static async Task<Guid> CreateCheckoutOrderViaGatewayAsync(string accessToken, Guid userId, decimal unitPrice)
    {
        var productName = $"Auth Contract Product {DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";

        var createProductResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/listing/products")
                {
                    Content = JsonContent.Create(new
                    {
                        sellerId = "11111111-1111-1111-1111-111111111111",
                        name = productName,
                        description = "auth-contract",
                        price = unitPrice,
                        currency = "RUB",
                        deliveryType = "Physical",
                        stockQuantity = 3
                    })
                };

                return request;
            },
            maxAttempts: 4);
        createProductResponse.EnsureSuccessStatusCode();

        var productJson = await createProductResponse.Content.ReadAsStringAsync();
        using var productDoc = JsonDocument.Parse(productJson);
        var productId = productDoc.RootElement.GetProperty("id").GetGuid();

        var addItemResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var addItemRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/order/cart/items")
                {
                    Content = JsonContent.Create(new
                    {
                        productId,
                        quantity = 1,
                        unitPrice,
                        currency = "RUB"
                    })
                };
                addItemRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                addItemRequest.Headers.Add("X-User-Id", userId.ToString());
                return addItemRequest;
            },
            maxAttempts: 4);
        addItemResponse.EnsureSuccessStatusCode();

        var checkoutResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/order/cart/checkout")
                {
                    Content = JsonContent.Create(new
                    {
                        addressId = "demo-address",
                        paymentMethod = "Wallet"
                    })
                };
                checkoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                checkoutRequest.Headers.Add("X-User-Id", userId.ToString());
                return checkoutRequest;
            },
            maxAttempts: 4);
        checkoutResponse.EnsureSuccessStatusCode();

        var checkoutJson = await checkoutResponse.Content.ReadAsStringAsync();
        using var checkoutDoc = JsonDocument.Parse(checkoutJson);
        return checkoutDoc.RootElement.GetProperty("orderId").GetGuid();
    }

    private static async Task<(string AccessToken, Guid UserId, string Login, string Email)> RegisterAndLoginAsync()
    {
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid().ToString("N")[..8];
        var login = $"auth_itest_{stamp}_{nonce}";
        var email = $"auth_itest_{stamp}_{nonce}@example.com";
        const string password = "Password123!";

        var registerResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
                {
                    Content = JsonContent.Create(new
                    {
                        login,
                        email,
                        password
                    })
                };

                return request;
            },
            maxAttempts: 4);
        Assert.True(
            registerResponse.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict or HttpStatusCode.InternalServerError,
            $"Unexpected register status code: {registerResponse.StatusCode}");

        var loginResponse = await SendGatewayWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
                {
                    Content = JsonContent.Create(new
                    {
                        email,
                        password
                    })
                };

                return request;
            },
            maxAttempts: 6);
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginJson);

        var accessToken = doc.RootElement.GetProperty("accessToken").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        var userId = doc.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        return (accessToken, userId, login, email);
    }

    private static async Task<HttpResponseMessage> SendGatewayWithRetryAsync(Func<HttpRequestMessage> requestFactory, int maxAttempts)
    {
        Exception? lastException = null;
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();

            try
            {
                var response = await GatewayClient.SendAsync(request);
                if (IsTransientStatusCode(response.StatusCode) && attempt < maxAttempts)
                {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        lastResponse?.Dispose();
        throw new InvalidOperationException("Gateway request failed after retries.", lastException);
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is TaskCanceledException or HttpRequestException;
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests
            || statusCode == HttpStatusCode.InternalServerError
            || statusCode == HttpStatusCode.BadGateway
            || statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.GatewayTimeout;
    }
}
