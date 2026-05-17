using System.Security.Claims;
using EzBias.Application.Features.Admin;
using EzBias.Application.Features.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminApplicationService _adminService;

    public AdminUsersController(IAdminApplicationService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? keyword, [FromQuery] string? role, [FromQuery] bool? isDeleted, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _adminService.GetUsersAsync(new AdminUserListQuery(keyword, role, isDeleted, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var result = await _adminService.GetUserDetailAsync(id, ct);
        if (!result.Success || result.Data is null) return NotFound(result.Error);
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCreateUserRequest request, CancellationToken ct)
    {
        var result = await _adminService.CreateUserAsync(request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
    {
        var result = await _adminService.UpdateUserAsync(id, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "User not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> SoftDelete([FromRoute] long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();

        var result = await _adminService.SoftDeleteUserAsync(id, adminId, ct);
        if (!result.Success)
        {
            if (result.Error == "User not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPut("{id:long}/restore")]
    public async Task<IActionResult> Restore([FromRoute] long id, CancellationToken ct)
    {
        var result = await _adminService.RestoreUserAsync(id, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "User not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

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
