namespace OrderService.API.Application.DTOs;

public sealed record CartDTO(
    
    Guid CartId,
    IReadOnlyCollection<CartItemDTO> Items,
    decimal TotalAmount);