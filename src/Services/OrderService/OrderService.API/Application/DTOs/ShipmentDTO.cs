using OrderService.API.Application.Validation;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Application.DTOs;


public sealed record ShipmentDTO(
    [NotEmptyGuid(ErrorMessage = "ShipmentId is required.")]
    Guid ShipmentId, 

    [NotEmptyGuid(ErrorMessage = "OrderId is required.")]
    Guid OrderId, 
    OrderStatus OrderStatus, 
    string? TrackingNumber);