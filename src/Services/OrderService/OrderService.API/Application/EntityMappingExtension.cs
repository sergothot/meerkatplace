using System.ComponentModel.DataAnnotations;
using OrderService.API.Application.Validation;
using OrderService.API.Domain.Entities;
using OrderService.API.Application.DTOs;
using Common.Shared.Domain.Enums;

namespace OrderService.API.Application;

public static class EntityMappingExtension
{
    public static CartItemDTO ToDto(this CartItem item)
    {
        return new CartItemDTO(
            item.Id,
            item.ProductId,
            item.Quantity,
            item.UnitPrice,
            item.Quantity * item.UnitPrice,
            item.Currency);
    }

    public static CartDTO ToDto(this Cart cart)
    {
        return new CartDTO(
            cart.Id,
            cart.BuyerId,
            cart.Items.Select(item => item.ToDto()).ToList(),
            cart.TotalAmount,
            cart.Currency);
        }
}