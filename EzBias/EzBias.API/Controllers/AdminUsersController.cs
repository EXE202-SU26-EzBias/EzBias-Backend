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
}
