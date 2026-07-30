using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Users;
using EzBias.Application.Features.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserProfileApplicationService _service;

    public UsersController(IUserProfileApplicationService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.GetMeAsync(userId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result, forceKind: ErrorKind.NotFound);
        return Ok(result.Value);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.UpdateMeAsync(userId, request, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result, forceKind: ErrorKind.NotFound);
        return Ok(result.Value);
    }

    [HttpDelete("by-email")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteUnverifiedByEmail([FromQuery] string email, CancellationToken ct)
    {
        var result = await _service.DeleteUnverifiedByEmailAsync(email, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result);

        return NoContent();
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
