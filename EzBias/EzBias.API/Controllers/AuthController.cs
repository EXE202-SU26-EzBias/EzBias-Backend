using System.Security.Claims;
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
        if (!result.Success || result.Data is null) return Conflict(result.Error);

        SetRefreshCookie(result.Data.RefreshToken);
        return Ok(ToAuthResponse(result.Data));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req, ct);
        if (!result.Success || result.Data is null) return Unauthorized(result.Error);

        SetRefreshCookie(result.Data.RefreshToken);
        return Ok(ToAuthResponse(result.Data));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? req, CancellationToken ct)
    {
        var tokenFromCookie = Request.Cookies[RefreshCookieName];
        var token = !string.IsNullOrWhiteSpace(tokenFromCookie) ? tokenFromCookie : req?.RefreshToken;

        var result = await _authService.RefreshAsync(new RefreshRequest(token), ct);
        if (!result.Success || result.Data is null) return Unauthorized(result.Error);

        SetRefreshCookie(result.Data.RefreshToken);
        return Ok(ToAuthResponse(result.Data));
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

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        if (!long.TryParse(sub, out var userId))
            return Unauthorized();

        var result = await _authService.MeAsync(userId, ct);
        if (!result.Success || result.Data is null) return Unauthorized();

        return Ok(result.Data);
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
}
