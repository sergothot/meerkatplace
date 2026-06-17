using Common.Shared.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;

namespace OrderService.API.Application.DTOs;

public sealed record CheckoutDTO(
    [NotEmptyGuid]
    Guid AddressId,

    [EnumDataType(typeof(Currency), ErrorMessage = "PaymentMethod is required.")]
    PaymentMethod PaymentMethod);