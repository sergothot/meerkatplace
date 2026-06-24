using UserService.API.Domain.Enums;

namespace UserService.API.Application.DTOs;

public record RegisterRequest(string Login, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record RegisterResponse(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<UserRole> Roles,
    DateTimeOffset CreatedAt);

public record AuthUserDto(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<UserRole> Roles);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthUserDto User);
