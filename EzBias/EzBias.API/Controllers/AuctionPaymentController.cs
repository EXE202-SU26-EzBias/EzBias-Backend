using System.Security.Claims;
using EzBias.Application.Features.Payments;
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
        if (order is null) return NotFound("Auction order not found.");
        if (order.UserId != userId) return Forbid();

        var payment = await _payments.GetPendingByAuctionIdAsync(auctionId, ct);
        if (payment is null) return NotFound("Pending payment not found.");

        var result = await _paymentService.ConfirmAsync(userId, payment.Id, ct);
        if (!result.Success || result.Data is null) return BadRequest(result.Error);
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
