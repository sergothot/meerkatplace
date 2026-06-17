using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;

namespace OrderService.API.Application.DTOs;

public sealed record CartItemDTO(
    [NotEmptyGuid]
    Guid ProductId,

    [GreaterThanZero(ErrorMessage = "Quantity must be greater than zero.")]
    int Quantity,

    [GreaterThanZero(ErrorMessage = "UnitPrice must be greater than zero.")]
    decimal UnitPrice,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency);