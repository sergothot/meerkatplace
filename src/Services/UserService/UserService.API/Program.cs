using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.API.Application.Auth;
using UserService.API.Application.Users;
using UserService.API.Domain.Entities;
using UserService.API.Domain.Enums;
using UserService.API.Infrastructure.Persistence;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey       = builder.Configuration["Jwt:Key"]!;
var jwtIssuer    = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience  = builder.Configuration["Jwt:Audience"]!;
var jwtExpiresIn = builder.Configuration.GetValue<int>("Jwt:ExpiresInSeconds", 3600);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

// ── Misc ──────────────────────────────────────────────────────────────────────

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "user-service" }));

// ── Auth ──────────────────────────────────────────────────────────────────────

app.MapPost("/auth/register", async (RegisterRequest request, UserDbContext db) =>
{
    var errors = AuthRequestValidator.ValidateRegister(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.Conflict(new { error = new { code = "DUPLICATE_EMAIL", message = "Email is already registered." } });

    if (await db.Users.AnyAsync(u => u.Login == request.Login))
        return Results.Conflict(new { error = new { code = "DUPLICATE_LOGIN", message = "Login is already taken." } });

    var user = new User
    {
        Login = request.Login,
        Email = request.Email,
        Roles = [UserRole.Buyer]
    };
    user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

    db.Users.Add(user);
    db.BuyerProfiles.Add(new BuyerProfile { UserId = user.Id });
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}",
        new RegisterResponse(user.Id, user.Login, user.Email, user.Roles, user.CreatedAt));
});

app.MapPost("/auth/login", async (LoginRequest request, UserDbContext db) =>
{
    var errors = AuthRequestValidator.ValidateLogin(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user is null)
        return Results.Unauthorized();

    var verifyResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (verifyResult == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var response = await BuildAuthResponseAsync(user, db, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn);
    return Results.Ok(response);
});

app.MapPost("/auth/refresh", async (RefreshRequest request, UserDbContext db) =>
{
    var errors = AuthRequestValidator.ValidateRefresh(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
    if (stored is null || stored.ExpiresAt <= DateTimeOffset.UtcNow)
    {
        if (stored is not null)
        {
            db.RefreshTokens.Remove(stored);
            await db.SaveChangesAsync();
        }
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(stored.UserId);
    if (user is null)
        return Results.Unauthorized();

    db.RefreshTokens.Remove(stored);
    var response = await BuildAuthResponseAsync(user, db, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn);
    return Results.Ok(response);
});

// ── Users ─────────────────────────────────────────────────────────────────────

app.MapGet("/users/me", async (HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId);
    return user is null ? Results.NotFound() : Results.Ok(ToProfileResponse(user));
}).RequireAuthorization();

app.MapPatch("/users/me", async (UpdateProfileRequest request, HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var errors = UserRequestValidator.ValidateUpdateProfile(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    if (request.Login is not null)
    {
        if (await db.Users.AnyAsync(u => u.Login == request.Login && u.Id != userId))
            return Results.Conflict(new { error = new { code = "DUPLICATE_LOGIN", message = "Login is already taken." } });
        user.Login = request.Login;
    }

    await db.SaveChangesAsync();
    return Results.Ok(ToProfileResponse(user));
}).RequireAuthorization();

app.MapPost("/users/me/roles/seller", async (CreateSellerProfileRequest request, HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var errors = UserRequestValidator.ValidateCreateSellerProfile(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    if (user.Roles.Contains(UserRole.Seller))
        return Results.Conflict(new { error = new { code = "ALREADY_SELLER", message = "User is already a seller." } });

    if (await db.SellerProfiles.AnyAsync(s => s.StoreName == request.StoreName))
        return Results.Conflict(new { error = new { code = "DUPLICATE_STORE_NAME", message = "Store name is already taken." } });

    var profile = new SellerProfile { UserId = user.Id, StoreName = request.StoreName };
    db.SellerProfiles.Add(profile);

    user.Roles = [.. user.Roles, UserRole.Seller];
    await db.SaveChangesAsync();

    return Results.Ok(new SellerProfileResponse(profile.Id, profile.UserId, profile.StoreName, profile.Rating));
}).RequireAuthorization();

// ── Addresses ─────────────────────────────────────────────────────────────────

app.MapGet("/users/me/addresses", async (HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var addresses = await db.Addresses
        .Where(a => a.UserId == userId)
        .ToListAsync();

    return Results.Ok(addresses.Select(ToAddressDto));
}).RequireAuthorization();

app.MapPost("/users/me/addresses", async (CreateAddressRequest request, HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var errors = UserRequestValidator.ValidateCreateAddress(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var address = new Address
    {
        UserId     = userId.Value,
        Country    = request.Country,
        City       = request.City,
        Street     = request.Street,
        PostalCode = request.PostalCode
    };
    db.Addresses.Add(address);
    await db.SaveChangesAsync();

    return Results.Created($"/users/me/addresses/{address.Id}", ToAddressDto(address));
}).RequireAuthorization();

app.MapPatch("/users/me/addresses/{addressId:guid}", async (Guid addressId, UpdateAddressRequest request, HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    if (address is null) return Results.NotFound();

    if (request.Country    is not null) address.Country    = request.Country;
    if (request.City       is not null) address.City       = request.City;
    if (request.Street     is not null) address.Street     = request.Street;
    if (request.PostalCode is not null) address.PostalCode = request.PostalCode;

    await db.SaveChangesAsync();
    return Results.Ok(ToAddressDto(address));
}).RequireAuthorization();

app.MapDelete("/users/me/addresses/{addressId:guid}", async (Guid addressId, HttpContext ctx, UserDbContext db) =>
{
    var userId = GetCurrentUserId(ctx);
    if (userId is null) return Results.Unauthorized();

    var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    if (address is null) return Results.NotFound();

    db.Addresses.Remove(address);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────

static Guid? GetCurrentUserId(HttpContext ctx)
{
    var claim = ctx.User.FindFirstValue("sub");
    return Guid.TryParse(claim, out var id) ? id : null;
}

static UserProfileResponse ToProfileResponse(User user) =>
    new(user.Id, user.Login, user.Email, user.Roles, user.CreatedAt);

static AddressDto ToAddressDto(Address a) =>
    new(a.Id, a.Country, a.City, a.Street, a.PostalCode);

static async Task<AuthResponse> BuildAuthResponseAsync(
    User user, UserDbContext db, string key, string issuer, string audience, int expiresIn)
{
    var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.Name,  user.Login),
        new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
    };
    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));

    var token = new JwtSecurityToken(
        issuer:             issuer,
        audience:           audience,
        claims:             claims,
        expires:            DateTime.UtcNow.AddSeconds(expiresIn),
        signingCredentials: credentials);

    var accessToken  = new JwtSecurityTokenHandler().WriteToken(token);
    var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    db.RefreshTokens.Add(new RefreshToken
    {
        Token     = refreshToken,
        UserId    = user.Id,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
    });
    await db.SaveChangesAsync();

    return new AuthResponse(
        accessToken,
        refreshToken,
        expiresIn,
        new AuthUserDto(user.Id, user.Login, user.Email, user.Roles));
}