namespace Common.Shared.Application.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
