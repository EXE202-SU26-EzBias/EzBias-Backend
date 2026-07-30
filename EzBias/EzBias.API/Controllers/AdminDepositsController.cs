using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits;
using EzBias.Application.Features.Deposits.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/admin/deposits")]
[Authorize(Roles = "Admin")]
public class AdminDepositsController : ControllerBase
{
    private readonly IDepositApplicationService _depositService;

    public AdminDepositsController(IDepositApplicationService depositService)
    {
        _depositService = depositService;
    }

    /// <summary>
    /// Get all Held deposits pending refund across all auctions.
    /// Admin can review these deposits and manually process refunds for losing bidders.
    /// </summary>
    [HttpGet("pending-refunds")]
    public async Task<IActionResult> GetPendingRefunds(CancellationToken ct)
    {
        var result = await _depositService.GetPendingRefundsAsync(ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result, notFoundAsBadRequest: true);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get detailed information about a specific deposit.
    /// Includes deposit state, auction details, user information, and payment reference.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetDepositDetail([FromRoute] long id, CancellationToken ct)
    {
        var result = await _depositService.GetDepositDetailAsync(id, ct);
        if (!result.IsSuccess || result.Value is null)
            return this.ToErrorActionResult(result);

        return Ok(result.Value);
    }

    /// <summary>
    /// Manually process a refund for a Held deposit.
    /// This is typically used for losing bidders whose deposits need to be refunded after auction closes.
    /// Transitions deposit from Held to Refunded, creates a Refund record, and notifies the user.
    /// </summary>
    [HttpPost("{id:long}/refund")]
    public async Task<IActionResult> ProcessManualRefund(
        [FromRoute] long id,
        [FromBody] ProcessManualRefundRequest request,
        CancellationToken ct)
    {
        var result = await _depositService.ProcessManualRefundAsync(id, request.Reason, ct);
        if (!result.IsSuccess)
            return this.ToErrorActionResult(result);

        return Ok(new { message = "Refund processed successfully." });
    }
}
