using EzBias.API.Infrastructure;
using EzBias.Application.Features.Contact;
using EzBias.Application.Features.Contact.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IContactApplicationService _contacts;

    public ContactController(IContactApplicationService contacts)
    {
        _contacts = contacts;
    }

    public record ContactRequest(string Name, string Email, string Subject, string Message);

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactRequest request, CancellationToken ct)
    {
        var result = await _contacts.SubmitAsync(
            new SubmitContactRequest(
                request.Name,
                request.Email,
                request.Subject,
                request.Message),
            ct);
        if (!result.IsSuccess || result.Value is null) return this.ToErrorActionResult(result);
        return Ok(result.Value);
    }
}
