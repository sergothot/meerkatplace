using UserService.API.Domain.Enums;

namespace UserService.API.Domain.Entities;

public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<UserRole> Roles { get; set; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
