using System.ComponentModel.DataAnnotations;
namespace OrderService.API.Application.Validation;
using System.ComponentModel.DataAnnotations;

public class GreaterThanZeroAttribute : ValidationAttribute
{
    public GreaterThanZeroAttribute()
    {
        ErrorMessage = "Must be greater than zero";
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            decimal d => d > 0,
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            float f => f > 0,
            _ => false
        };
    }
}