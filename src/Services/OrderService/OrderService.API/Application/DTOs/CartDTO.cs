using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;

namespace OrderService.API.Application.DTOs;

public sealed record CartDTO(
    [NotEmptyGuid(ErrorMessage = "CartId is required.")]
    Guid CartId,

    [NotEmptyGuid(ErrorMessage = "BuyerId is required.")]
    Guid BuyerId,

    IReadOnlyList<CartItemDTO> Items,
    decimal TotalAmount,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency);
    
