using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EzBias.API.Mappings;
using EzBias.Application.Common.Results;
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
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result, notFoundAsBadRequest: true);
        return Ok(result.Value);
    }

    [HttpGet("{paymentId:long}")]
    public async Task<IActionResult> GetStatus([FromRoute] long paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _paymentService.GetStatusAsync(userId, paymentId, ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("{paymentId:long}/manual-confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManualConfirm([FromRoute] long paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var adminId)) return Unauthorized();

        var result = await _paymentService.ConfirmManualAsync(adminId, paymentId, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result);

        return Ok(new { ok = true, paymentId, message = "Payment confirmed manually." });
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false);
        var rawBody = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest(new { message = "Empty webhook body." });

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
            return BadRequest(new { message = "Invalid JSON payload." });
        }

        if (request is null)
            return BadRequest(new { message = "Invalid webhook payload." });

        var signature = Request.Headers["X-SePay-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-SePay-Timestamp"].FirstOrDefault();

        var result = await _paymentService.HandleSePayWebhookAsync(request, rawBody, signature, timestamp, ct);
        if (!result.IsSuccess) return this.ToErrorActionResult(result, notFoundAsBadRequest: true);

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
