namespace UserService.API.Application.Auth;

public interface IAuthService
{
    Task<IResult> RegisterAsync(RegisterRequest request);

    Task<IResult> LoginAsync(
        LoginRequest request,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpiresIn);

    Task<IResult> RefreshAsync(
        RefreshRequest request,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        int jwtExpiresIn);
}
