using System.Security.Claims;
using EzBias.API.Hubs;
using EzBias.API.Infrastructure;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auctions;
using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionBiddingApplicationService _service;
    private readonly IHubContext<AuctionHub> _auctionHub;

    public AuctionsController(IAuctionBiddingApplicationService service, IHubContext<AuctionHub> auctionHub)
    {
        _service = service;
        _auctionHub = auctionHub;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuctionStatus? status, CancellationToken ct)
    {
        var data = await _service.GetPublicAuctionsAsync(status, ct);
        return Ok(data);
    }

    [HttpGet("{auctionId:long}")]
    public async Task<IActionResult> GetById([FromRoute] long auctionId, CancellationToken ct)
    {
        var result = await _service.GetDetailAsync(auctionId, ct);
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed);
        return Ok(typed.Value);
    }

    [HttpGet("{auctionId:long}/bids/history")]
    public async Task<IActionResult> GetBidHistory([FromRoute] long auctionId, CancellationToken ct)
    {
        var data = await _service.GetBidHistoryAsync(auctionId, ct);
        return Ok(data);
    }

    [Authorize]
    [HttpPost("{auctionId:long}/bids")]
    public async Task<IActionResult> PlaceBid([FromRoute] long auctionId, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _service.PlaceBidAsync(userId, auctionId, request, ct);
        var typed = result.ToResult();
        if (!typed.IsSuccess || typed.Value is null) return this.ToErrorActionResult(typed);

        // Push realtime event to all viewers of this auction
        await _auctionHub.Clients
            .Group(AuctionHub.AuctionGroup(auctionId))
            .SendAsync("BidPlaced", new
            {
                auctionId,
                bidId      = typed.Value.BidId,
                amount     = typed.Value.Amount,
                currentBid = typed.Value.CurrentBid,
                status     = typed.Value.Status.ToString(),
                placedAt   = DateTimeOffset.UtcNow
            }, ct);

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
