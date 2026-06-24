using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Application.DTOs;

public sealed record OrderDTO(
    [NotEmptyGuid(ErrorMessage = "OrderId is required.")]
    Guid OrderId,
    OrderStatus OrderStatus,
    decimal TotalAmount,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency,
    DateTimeOffset CreatedAt);