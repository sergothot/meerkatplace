using System.Security.Claims;
using UserService.API.Application.Auth;
using UserService.API.Application.Users;

namespace UserService.API.Presentation.Endpoints;

public static class UserServiceEndpoints
{
    public static void MapUserServiceEndpoints(
        this WebApplication app,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpiresIn)
    {
        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "user-service" }));

        var auth = app.MapGroup("/auth");
        auth.MapPost("/register", (RegisterRequest request, IAuthService authService) =>
            authService.RegisterAsync(request))
            .WithSummary("Register user")
            .WithDescription("Creates a new buyer account and returns basic profile data.");

        auth.MapPost("/login", (LoginRequest request, IAuthService authService) =>
            authService.LoginAsync(request, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn))
            .WithSummary("Login")
            .WithDescription("Authenticates a user and returns access and refresh tokens.");

        auth.MapPost("/refresh", (RefreshRequest request, IAuthService authService) =>
            authService.RefreshAsync(request, jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn))
            .WithSummary("Refresh token")
            .WithDescription("Exchanges a refresh token for a new access token pair.");

        var me = app.MapGroup("/users/me").RequireAuthorization();
        me.MapGet("", (HttpContext ctx, IUserProfileService users) =>
            users.GetCurrentProfileAsync(GetCurrentUserId(ctx)))
            .WithSummary("Get current profile")
            .WithDescription("Returns current authenticated user profile.");

        me.MapPatch("", (UpdateProfileRequest request, HttpContext ctx, IUserProfileService users) =>
            users.UpdateProfileAsync(GetCurrentUserId(ctx), request));

        me.MapPost("/roles/seller", (CreateSellerProfileRequest request, HttpContext ctx, IUserProfileService users) =>
            users.CreateSellerProfileAsync(GetCurrentUserId(ctx), request));

        var addressRoutes = me.MapGroup("/addresses");
        addressRoutes.MapGet("", (HttpContext ctx, IUserProfileService users) =>
            users.GetAddressesAsync(GetCurrentUserId(ctx)))
            .WithSummary("List addresses")
            .WithDescription("Returns delivery addresses for current user.");

        addressRoutes.MapPost("", (CreateAddressRequest request, HttpContext ctx, IUserProfileService users) =>
            users.CreateAddressAsync(GetCurrentUserId(ctx), request))
            .WithSummary("Create address")
            .WithDescription("Creates a delivery address for current user.");

        addressRoutes.MapPatch("/{addressId:guid}", (Guid addressId, UpdateAddressRequest request, HttpContext ctx, IUserProfileService users) =>
            users.UpdateAddressAsync(GetCurrentUserId(ctx), addressId, request));

        addressRoutes.MapDelete("/{addressId:guid}", (Guid addressId, HttpContext ctx, IUserProfileService users) =>
            users.DeleteAddressAsync(GetCurrentUserId(ctx), addressId));
    }

    private static Guid? GetCurrentUserId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirstValue("sub")
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
