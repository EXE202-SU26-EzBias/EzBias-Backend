using System.Security.Claims;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
using EzBias.Application.Features.Auctions;
using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/seller/auctions")]
[Authorize]
public class SellerAuctionsController : ControllerBase
{
    private readonly ISellerAuctionApplicationService _service;

    public SellerAuctionsController(ISellerAuctionApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] AuctionStatus? status, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var data = await _service.GetMyAuctionsAsync(userId, status, ct);
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("{auctionId:long}/publish")]
    public async Task<IActionResult> Publish([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.PublishAsync(userId, auctionId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("{auctionId:long}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] long auctionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CancelAsync(userId, auctionId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("{auctionId:long}/relist")]
    public async Task<IActionResult> Relist([FromRoute] long auctionId, [FromBody] RelistAuctionRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.RelistAsync(userId, auctionId, request, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
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
