using System.Security.Claims;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Payments;
using EzBias.Application.Features.Payments.Dtos;
using EzBias.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auctions")]
[Authorize]
public class AuctionPaymentController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentApplicationService _paymentService;

    public AuctionPaymentController(IOrderRepository orders, IPaymentRepository payments, IPaymentApplicationService paymentService)
    {
        _orders = orders;
        _payments = payments;
        _paymentService = paymentService;
    }

    [HttpPost("{auctionId:long}/pay")]
    public async Task<IActionResult> Pay([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var order = await _orders.GetByAuctionIdAsync(auctionId, ct);
        if (order is null) return NotFound(new { message = "Auction order not found." });
        if (order.UserId != userId) return Forbid();

        var payment = await _payments.GetPendingByAuctionIdAsync(auctionId, ct);
        if (payment is null) return NotFound(new { message = "Pending payment not found." });

        var hook = await _paymentService.HandleWebhookAsync(new PaymentWebhookRequest(
            payment.Reference,
            payment.ProviderTxnId,
            payment.TransferContent,
            payment.Payload), "{}", null, null, ct);
        if (!hook.IsSuccess)
            return this.ToErrorActionResult(hook, notFoundAsBadRequest: true);

        var status = await _paymentService.GetStatusAsync(userId, payment.Id, ct);
        if (!status.IsSuccess || status.Value is null)
            return this.ToErrorActionResult(status, notFoundAsBadRequest: true);
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
