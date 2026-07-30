using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Admin;
using EzBias.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminApplicationService _adminService;
    private readonly IOrderApplicationService _orderService;

    public AdminDashboardController(IAdminApplicationService adminService, IOrderApplicationService orderService)
    {
        _adminService = adminService;
        _orderService = orderService;
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

    [HttpGet("orders/{id:long}")]
    public async Task<IActionResult> OrderDetail([FromRoute] long id, CancellationToken ct)
    {
        var result = await _orderService.GetDetailForAdminAsync(id, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }
}
