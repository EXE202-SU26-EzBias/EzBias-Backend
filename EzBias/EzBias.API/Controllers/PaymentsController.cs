using System.Security.Claims;
using System.Text;
using EzBias.Application.Features.Payments;
using EzBias.Application.Features.Payments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentApplicationService _paymentService;

    public PaymentsController(IPaymentApplicationService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _paymentService.CreateAsync(userId, request, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [HttpGet("{paymentId:long}")]
    public async Task<IActionResult> GetStatus([FromRoute] long paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _paymentService.GetStatusAsync(userId, paymentId, ct);
        if (!result.Success || result.Data is null)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Payment not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] SePayWebhookPayload request, CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-SePay-Signature"].FirstOrDefault();

        var result = await _paymentService.HandleSePayWebhookAsync(request, rawBody, signature, ct);
        if (!result.Success)
        {
            if (result.Error == "Invalid webhook signature.") return Unauthorized(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{paymentId:long}/mark-paid")]
    public async Task<IActionResult> MarkPaidManual([FromRoute] long paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _paymentService.MarkPaidManualAsync(userId, paymentId, ct);
        if (!result.Success)
        {
            if (result.Error == "Forbidden.") return Forbid();
            if (result.Error == "Payment not found.") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(new { paymentId, status = "Paid" });
    }


    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
