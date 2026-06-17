using System.ComponentModel.DataAnnotations;
namespace OrderService.API.Application.Validation;

public class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
    {
        ErrorMessage = "ID is required";
    }

    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}