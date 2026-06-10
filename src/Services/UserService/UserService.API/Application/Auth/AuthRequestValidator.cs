using System.Text.RegularExpressions;

namespace UserService.API.Application.Auth;

public static partial class AuthRequestValidator
{
    private static readonly Regex EmailRegex = EmailPattern();

    public static Dictionary<string, string[]> ValidateRegister(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Length < 3 || request.Login.Length > 50)
        {
            errors["login"] = new[] { "Login must be between 3 and 50 characters." };
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email))
        {
            errors["email"] = new[] { "Email must be a valid email address." };
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || request.Password.Length > 128)
        {
            errors["password"] = new[] { "Password must be between 8 and 128 characters." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateLogin(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email))
        {
            errors["email"] = new[] { "Email must be a valid email address." };
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = new[] { "Password is required." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateRefresh(RefreshRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            errors["refreshToken"] = new[] { "Refresh token is required." };
        }

        return errors;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();
}
