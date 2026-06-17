using OrderService.API.Application.Validation;

namespace OrderService.API.Application.DTOs;
public sealed record UpdateCartItemDTO(

    [GreaterThanZero(ErrorMessage = "Quantity must be greater than zero.")]
    int Quantity);