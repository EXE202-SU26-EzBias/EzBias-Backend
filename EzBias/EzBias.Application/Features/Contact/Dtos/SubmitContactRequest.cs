namespace EzBias.Application.Features.Contact.Dtos;

public sealed record SubmitContactRequest(
    string Name,
    string Email,
    string Subject,
    string Message);

public sealed record ContactSubmissionResponse(long Id, string Message);
