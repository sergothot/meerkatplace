using UserService.API.Domain.Entities;

namespace UserService.UnitTests;

public class AddressDomainTests
{
    [Fact]
    public void Create_WithBlankCity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Address.Create(Guid.NewGuid(), "RU", " ", "Street", "101000"));
    }

    [Fact]
    public void Create_TrimsValues()
    {
        var address = Address.Create(Guid.NewGuid(), " RU ", " Moscow ", " Tverskaya ", " 101000 ");

        Assert.Equal("RU", address.Country);
        Assert.Equal("Moscow", address.City);
        Assert.Equal("Tverskaya", address.Street);
        Assert.Equal("101000", address.PostalCode);
    }

    [Fact]
    public void Update_WithBlankStreet_Throws()
    {
        var address = Address.Create(Guid.NewGuid(), "RU", "Moscow", "Tverskaya", "101000");

        Assert.Throws<InvalidOperationException>(() =>
            address.Update("RU", "Moscow", " ", "101000"));
    }
}
