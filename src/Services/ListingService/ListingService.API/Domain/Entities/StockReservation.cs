using ListingService.API.Domain.Enums;

namespace ListingService.API.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public StockReservationStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static StockReservation CreateReserved(Guid orderId)
    {
        return new StockReservation
        {
            OrderId = orderId,
            Status = StockReservationStatus.Reserved
        };
    }

    public static StockReservation CreateFailed(Guid orderId, string reason)
    {
        return new StockReservation
        {
            OrderId = orderId,
            Status = StockReservationStatus.Failed,
            FailureReason = reason
        };
    }

    public void MarkReleased()
    {
        if (Status != StockReservationStatus.Reserved)
        {
            return;
        }

        Status = StockReservationStatus.Released;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}