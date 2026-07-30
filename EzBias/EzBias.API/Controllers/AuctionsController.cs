using System.Security.Claims;
using EzBias.API.Hubs;
using EzBias.API.Mappings;
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
    private readonly ILogger<AuctionsController> _logger;

    public AuctionsController(
        IAuctionBiddingApplicationService service,
        IHubContext<AuctionHub> auctionHub,
        ILogger<AuctionsController> logger)
    {
        _service = service;
        _auctionHub = auctionHub;
        _logger = logger;
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);

        // Push realtime event to all viewers of this auction
        try
        {
            await _auctionHub.Clients
                .Group(AuctionHub.AuctionGroup(auctionId))
                .SendAsync("BidPlaced", new
                {
                    auctionId,
                    bidId      = result.Value.BidId,
                    amount     = result.Value.Amount,
                    currentBid = result.Value.CurrentBid,
                    status     = result.Value.Status.ToString(),
                    placedAt   = DateTimeOffset.UtcNow
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Bid {BidId} committed but BidPlaced broadcast failed for auction {AuctionId}.",
                result.Value.BidId,
                auctionId);
        }

        return Ok(result.Value);
    }

    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
