namespace ListingService.API.Domain.Entities;

public class Review
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SellerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
