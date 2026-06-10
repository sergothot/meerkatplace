namespace UserService.API.Domain.Entities;

public class SellerProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}
