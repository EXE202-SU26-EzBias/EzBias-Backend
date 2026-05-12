using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false);
        var rawBody = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest("Empty webhook body.");

        SePayWebhookPayload? request;
        try
        {
            request = JsonSerializer.Deserialize<SePayWebhookPayload>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return BadRequest("Invalid JSON payload.");
        }

        if (request is null)
            return BadRequest("Invalid webhook payload.");

        var signature = Request.Headers["X-SePay-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-SePay-Timestamp"].FirstOrDefault();

        var result = await _paymentService.HandleSePayWebhookAsync(request, rawBody, signature, timestamp, ct);
        if (!result.Success)
        {
            if (result.Error == "Invalid webhook signature.") return Unauthorized(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(new { ok = true });
    }


    private bool TryGetUserId(out long userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");

        return long.TryParse(sub, out userId);
    }
}
