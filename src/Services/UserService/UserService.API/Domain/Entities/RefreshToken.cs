namespace UserService.API.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
