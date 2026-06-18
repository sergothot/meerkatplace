using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Application.DTOs;

public sealed record CheckoutDTO(
    [NotEmptyGuid(ErrorMessage = "OrderId is required.")]
    Guid OrderId,

    [NotEmptyGuid(ErrorMessage = "AddressId is required." )]
    Guid AddressId,

    OrderStatus OrderStatus,

    decimal Amount,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency,
    bool RequiresPayment,
    string PaymentMethod);
