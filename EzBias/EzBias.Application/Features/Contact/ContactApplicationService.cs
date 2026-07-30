using EzBias.Application.Common.Results;
using EzBias.Application.Features.Contact.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Contact;

public sealed class ContactApplicationService : IContactApplicationService
{
    private readonly IContactRepository _contacts;
    private readonly IUnitOfWork _uow;

    public ContactApplicationService(IContactRepository contacts, IUnitOfWork uow)
    {
        _contacts = contacts;
        _uow = uow;
    }

    public async Task<Result<ContactSubmissionResponse>> SubmitAsync(
        SubmitContactRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Subject)
            || string.IsNullOrWhiteSpace(request.Message))
            return Result<ContactSubmissionResponse>.Fail("All fields are required.", ApplicationErrorCode.Validation);

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false
        };

        _contacts.Add(message);
        await _uow.SaveChangesAsync(ct);

        return Result<ContactSubmissionResponse>.Ok(
            new ContactSubmissionResponse(message.Id, "Contact message submitted."));
    }
}
