namespace UserService.API.Application.Auth;

public record RegisterRequest(string Login, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record RegisterResponse(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);

public record AuthUserDto(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<string> Roles);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthUserDto User);

public record AuthUser(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    string PasswordHash);
