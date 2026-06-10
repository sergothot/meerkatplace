namespace OrderService.API.Domain.Enums;

public enum FulfillmentStatus
{
    Draft = 1,
    Placed = 2,
    Paid = 3,
    Fulfilled = 4,
    Completed = 5,
    Cancelled = 6
}