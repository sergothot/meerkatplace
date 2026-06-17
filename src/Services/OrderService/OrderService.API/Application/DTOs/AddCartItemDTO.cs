using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;
namespace OrderService.API.Application.DTOs;

public sealed record AddCartItemDTO(
    [NotEmptyGuid]
    Guid ProductId,

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    int Quantity);