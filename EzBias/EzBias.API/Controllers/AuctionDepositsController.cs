using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auctions")]
[Authorize]
public class AuctionDepositsController : ControllerBase
{
    private readonly IDepositApplicationService _deposits;

    public AuctionDepositsController(IDepositApplicationService deposits)
    {
        _deposits = deposits;
    }

    [HttpPost("{auctionId:long}/deposit")]
    public async Task<IActionResult> InitiateDeposit([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deposits.InitiateDepositAsync(userId, auctionId, ct);
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed);
        return Ok(typed.Value);
    }

    [HttpGet("{auctionId:long}/deposit")]
    public async Task<IActionResult> GetMyDepositStatus([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deposits.GetMyDepositStatusAsync(userId, auctionId, ct);
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed);
        return Ok(typed.Value);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
