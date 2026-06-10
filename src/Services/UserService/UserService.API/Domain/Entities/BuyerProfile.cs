namespace UserService.API.Domain.Entities;

public class BuyerProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? ActiveCartId { get; set; }
}
