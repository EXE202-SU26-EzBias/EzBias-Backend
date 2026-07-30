namespace EzBias.Application.Common.Results;

public sealed record ApplicationError(
    ApplicationErrorCode Code,
    ErrorKind Kind,
    string Message)
{
    public static ApplicationError Create(
        ApplicationErrorCode code,
        string message)
        => new(
            code,
            code switch
            {
                ApplicationErrorCode.ResourceNotFound => ErrorKind.NotFound,
                ApplicationErrorCode.Forbidden => ErrorKind.Forbidden,
                ApplicationErrorCode.Unauthorized
                    or ApplicationErrorCode.InvalidWebhookSignature => ErrorKind.Unauthorized,
                ApplicationErrorCode.Conflict
                    or ApplicationErrorCode.PaymentAlreadyPaid
                    or ApplicationErrorCode.ConcurrencyConflict => ErrorKind.Conflict,
                _ => ErrorKind.Validation
            },
            message);
}
