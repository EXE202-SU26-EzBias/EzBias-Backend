namespace EzBias.Application.Common.Results;

public enum ApplicationErrorCode
{
    Validation = 1,
    ResourceNotFound = 2,
    Forbidden = 3,
    Unauthorized = 4,
    Conflict = 5,
    PaymentAlreadyPaid = 6,
    InvalidWebhookSignature = 7,
    ConcurrencyConflict = 8,
    InvalidStateTransition = 9
}
