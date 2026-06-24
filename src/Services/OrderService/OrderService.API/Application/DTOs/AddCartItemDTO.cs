using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;
using Common.Shared.Domain.Enums;
namespace OrderService.API.Application.DTOs;

public sealed record AddCartItemDTO(
    [NotEmptyGuid(ErrorMessage = "ProductId is required.")]
    Guid ProductId,

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    int Quantity,
    
    [GreaterThanZero(ErrorMessage = "UnitPrice must be greater than zero.")]
    decimal UnitPrice,

    [EnumDataType(typeof(Currency), ErrorMessage = "Currency must be a 3-5 character ISO-like code.")]
    Currency Currency);

  