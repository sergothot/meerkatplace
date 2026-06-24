using PaymentService.API.Domain.Entities;
using PaymentService.API.Domain.Enums;

namespace PaymentService.UnitTests;

public class PaymentTransactionDomainTests
{
    [Fact]
    public void MarkSucceeded_FromPending_Succeeds()
    {
        var payment = new PaymentTransaction();

        payment.MarkSucceeded();

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void MarkRefunded_FromSucceeded_Succeeds()
    {
        var payment = new PaymentTransaction();
        payment.MarkSucceeded();

        payment.MarkRefunded();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void MarkRefunded_FromPending_Throws()
    {
        var payment = new PaymentTransaction();

        Assert.Throws<InvalidOperationException>(payment.MarkRefunded);
    }
}
