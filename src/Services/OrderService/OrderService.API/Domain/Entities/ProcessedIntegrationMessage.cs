namespace OrderService.API.Domain.Entities;

public class ProcessedIntegrationMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}