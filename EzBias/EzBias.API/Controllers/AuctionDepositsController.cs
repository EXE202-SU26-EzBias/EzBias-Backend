using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits;
using EzBias.Application.Features.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auctions")]
[Authorize]
public class AuctionDepositsController : ControllerBase
{
    private readonly IDepositApplicationService _deposits;
    private readonly IPaymentApplicationService _payments;

    public AuctionDepositsController(
        IDepositApplicationService deposits,
        IPaymentApplicationService payments)
    {
        _deposits = deposits;
        _payments = payments;
    }

    [HttpPost("{auctionId:long}/deposit")]
    public async Task<IActionResult> InitiateDeposit([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deposits.InitiateDepositAsync(userId, auctionId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpGet("{auctionId:long}/deposit")]
    public async Task<IActionResult> GetMyDepositStatus([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deposits.GetMyDepositStatusAsync(userId, auctionId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("{auctionId:long}/deposit/confirm")]
    public async Task<IActionResult> ConfirmDepositPayment([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var confirmation = await _payments.ConfirmAuctionDepositAsync(userId, auctionId, ct);
        if (!confirmation.IsSuccess) return this.ToErrorActionResult(confirmation);

        var status = await _deposits.GetMyDepositStatusAsync(userId, auctionId, ct);
        if (!status.IsSuccess || status.Value is null) return this.ToErrorActionResult(status);

        return Ok(status.Value);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out userId);
    }
}
