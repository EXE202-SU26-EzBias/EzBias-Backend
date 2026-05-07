using EzBias.Domain.Entities;
using EzBias.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly EzBiasDbContext _db;

    public ContactController(EzBiasDbContext db)
    {
        _db = db;
    }

    public record ContactRequest(string Name, string Email, string Subject, string Message);

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("All fields are required.");

        var msg = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false
        };

        _db.ContactMessages.Add(msg);
        await _db.SaveChangesAsync(ct);

        return Ok(new { msg.Id, Message = "Contact message submitted." });
    }
}
