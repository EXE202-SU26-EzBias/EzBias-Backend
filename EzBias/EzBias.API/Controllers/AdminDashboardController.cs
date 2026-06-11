using EzBias.Application.Features.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminApplicationService _adminService;

    public AdminDashboardController(IAdminApplicationService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var data = await _adminService.GetDashboardOverviewAsync(ct);
        return Ok(data);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(CancellationToken ct)
    {
        var data = await _adminService.GetTransactionsAsync(ct);
        return Ok(data);
    }
}
