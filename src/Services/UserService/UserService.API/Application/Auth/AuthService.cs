using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Common.Shared.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Domain.Enums;

namespace UserService.API.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IBuyerProfileRepository buyerProfiles,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<IResult> RegisterAsync(RegisterRequest request)
    {
        var errors = AuthRequestValidator.ValidateRegister(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        if (await users.AnyByEmailAsync(request.Email))
            return Results.Conflict(new { error = new { code = "DUPLICATE_EMAIL", message = "Email is already registered." } });

        if (await users.AnyByLoginAsync(request.Login))
            return Results.Conflict(new { error = new { code = "DUPLICATE_LOGIN", message = "Login is already taken." } });

        var user = new User
        {
            Login = request.Login,
            Email = request.Email,
            Roles = [UserRole.Buyer]
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await users.AddAsync(user);
        await buyerProfiles.AddAsync(new BuyerProfile { UserId = user.Id });
        await unitOfWork.SaveChangesAsync();

        return Results.Created($"/users/{user.Id}",
            new RegisterResponse(user.Id, user.Login, user.Email, user.Roles, user.CreatedAt));
    }

    public async Task<IResult> LoginAsync(
        LoginRequest request,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpiresIn)
    {
        var errors = AuthRequestValidator.ValidateLogin(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var user = await users.GetByEmailAsync(request.Email);
        if (user is null)
            return Results.Unauthorized();

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Results.Unauthorized();

        var response = await BuildAuthResponseAsync(user, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn);
        return Results.Ok(response);
    }

    public async Task<IResult> RefreshAsync(
        RefreshRequest request,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpiresIn)
    {
        var errors = AuthRequestValidator.ValidateRefresh(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var stored = await refreshTokens.GetByTokenAsync(request.RefreshToken);
        if (stored is null || stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            if (stored is not null)
            {
                refreshTokens.Remove(stored);
                await unitOfWork.SaveChangesAsync();
            }

            return Results.Unauthorized();
        }

        var user = await users.GetByIdAsync(stored.UserId);
        if (user is null)
            return Results.Unauthorized();

        refreshTokens.Remove(stored);
        var response = await BuildAuthResponseAsync(user, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn);
        return Results.Ok(response);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        User user,
        string key,
        string issuer,
        string audience,
        int expiresIn)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Login),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresIn),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await refreshTokens.AddAsync(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await unitOfWork.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresIn,
            new AuthUserDto(user.Id, user.Login, user.Email, user.Roles));
    }
}
