using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using UserService.API.Application.Auth;
using UserService.API.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();

var usersByEmail = new ConcurrentDictionary<string, AuthUser>(StringComparer.OrdinalIgnoreCase);
var usersByLogin = new ConcurrentDictionary<string, AuthUser>(StringComparer.OrdinalIgnoreCase);
var refreshTokens = new ConcurrentDictionary<string, Guid>(StringComparer.Ordinal);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "user-service" }));
app.MapPost("/auth/register", (RegisterRequest request) =>
{
    var errors = AuthRequestValidator.ValidateRegister(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    if (usersByEmail.ContainsKey(request.Email))
    {
        return Results.Conflict(new
        {
            error = new
            {
                code = "DUPLICATE_EMAIL",
                message = "Email is already registered."
            }
        });
    }

    if (usersByLogin.ContainsKey(request.Login))
    {
        return Results.Conflict(new
        {
            error = new
            {
                code = "DUPLICATE_LOGIN",
                message = "Login is already registered."
            }
        });
    }

    var now = DateTimeOffset.UtcNow;
    var user = new AuthUser(
        Guid.NewGuid(),
        request.Login,
        request.Email,
        new[] { UserRole.Buyer },
        now,
        HashPassword(request.Password));

    usersByEmail[user.Email] = user;
    usersByLogin[user.Login] = user;

    var response = new RegisterResponse(
        user.Id,
        user.Login,
        user.Email,
        user.Roles,
        user.CreatedAt);

    return Results.Created($"/users/{user.Id}", response);
});

app.MapPost("/auth/login", (LoginRequest request) =>
{
    var errors = AuthRequestValidator.ValidateLogin(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    if (!usersByEmail.TryGetValue(request.Email, out var user))
    {
        return Results.Unauthorized();
    }

    if (!string.Equals(user.PasswordHash, HashPassword(request.Password), StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    var response = BuildAuthResponse(user, refreshTokens);
    return Results.Ok(response);
});

app.MapPost("/auth/refresh", (RefreshRequest request) =>
{
    var errors = AuthRequestValidator.ValidateRefresh(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    if (!refreshTokens.TryRemove(request.RefreshToken, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = usersByEmail.Values.FirstOrDefault(u => u.Id == userId);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var response = BuildAuthResponse(user, refreshTokens);
    return Results.Ok(response);
});

app.Run();

static AuthResponse BuildAuthResponse(AuthUser user, ConcurrentDictionary<string, Guid> refreshTokens)
{
    var accessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    var expiresInSeconds = 3600;

    refreshTokens[refreshToken] = user.Id;

    return new AuthResponse(
        accessToken,
        refreshToken,
        expiresInSeconds,
        new AuthUserDto(user.Id, user.Login, user.Email, user.Roles));
}

static string HashPassword(string password)
{
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
}