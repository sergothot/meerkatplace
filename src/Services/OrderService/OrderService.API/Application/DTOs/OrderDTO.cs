using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;

namespace OrderService.API.Application.DTOs;

public sealed record OrderDTO(
    [NotEmptyGuid]
    Guid OrderId,
    OrderStatus OrderStatus,
    decimal TotalAmount,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency,
    DateTime CreatedAt);