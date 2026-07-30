using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auth;
using EzBias.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "ezbias_refresh_token";
    private readonly IAuthApplicationService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthApplicationService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(req, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, forceKind: ErrorKind.Conflict);

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(ToAuthResponse(result.Value));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, forceKind: ErrorKind.Unauthorized);

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(ToAuthResponse(result.Value));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? req, CancellationToken ct)
    {
        var tokenFromCookie = Request.Cookies[RefreshCookieName];
        var token = !string.IsNullOrWhiteSpace(tokenFromCookie) ? tokenFromCookie : req?.RefreshToken;

        var result = await _authService.RefreshAsync(new RefreshRequest(token), ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, forceKind: ErrorKind.Unauthorized);

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(ToAuthResponse(result.Value));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? req, CancellationToken ct)
    {
        var tokenFromCookie = Request.Cookies[RefreshCookieName];
        var token = !string.IsNullOrWhiteSpace(tokenFromCookie) ? tokenFromCookie : req?.RefreshToken;

        await _authService.LogoutAsync(new RefreshRequest(token), ct);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        var result = await _authService.ForgotPasswordAsync(req, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, forceKind: ErrorKind.Validation);

        return Ok(new SimpleMessageResponse("If the email exists, a password reset code has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(req, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, forceKind: ErrorKind.Validation);

        return Ok(new SimpleMessageResponse("Password has been reset."));
    }

    [HttpPost("email-verification/request")]
    public async Task<IActionResult> RequestEmailVerification(RequestEmailVerificationRequest req, CancellationToken ct)
    {
        var result = await _authService.RequestEmailVerificationAsync(req, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, forceKind: ErrorKind.Validation);

        return Ok(new SimpleMessageResponse("If the email exists and is not verified, a verification code has been sent."));
    }

    [HttpPost("email-verification/verify")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest req, CancellationToken ct)
    {
        var result = await _authService.VerifyEmailAsync(req, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, forceKind: ErrorKind.Validation);

        return Ok(new SimpleMessageResponse("Email has been verified."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        if (!long.TryParse(sub, out var userId))
            return Unauthorized(ToMessage("Unauthorized."));

        var result = await _authService.MeAsync(userId, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, forceKind: ErrorKind.Unauthorized);

        return Ok(result.Value);
    }

    private void SetRefreshCookie(string refreshToken)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(14),
            Path = "/api/auth"
        };

        Response.Cookies.Append(RefreshCookieName, refreshToken, options);
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = "/api/auth",
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None
        });
    }

    private static AuthResponse ToAuthResponse(AuthResult data)
        => new(data.AccessToken, data.ExpiresInSeconds, data.UserId, data.Username, data.Email, data.Role);

    private static SimpleMessageResponse ToMessage(string? message)
        => new(string.IsNullOrWhiteSpace(message) ? "Request failed." : message);
}
