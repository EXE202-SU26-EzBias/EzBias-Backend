using EzBias.Application.Common.Results;
using EzBias.Application.Features.Contact.Dtos;

namespace EzBias.Application.Features.Contact;

public interface IContactApplicationService
{
    Task<Result<ContactSubmissionResponse>> SubmitAsync(
        SubmitContactRequest request,
        CancellationToken ct);
}
