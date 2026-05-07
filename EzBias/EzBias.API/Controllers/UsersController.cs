using System.Security.Claims;
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
        if (!result.Success || result.Data is null) return NotFound(result.Error);
        return Ok(result.Data);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.UpdateMeAsync(userId, request, ct);
        if (!result.Success || result.Data is null) return NotFound(result.Error);
        return Ok(result.Data);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
