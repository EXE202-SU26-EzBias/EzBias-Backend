namespace EzBias.Application.Common.Results;

public static class ApplicationErrorCatalog
{
    public static ApplicationError FromMessage(string? message, ApplicationErrorCode? preferredCode = null)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "Request could not be completed." : message;

        if (text.Equals("Forbidden.", StringComparison.OrdinalIgnoreCase))
            return new(ApplicationErrorCode.Forbidden, ErrorKind.Forbidden, text);
        if (text.Equals("Unauthorized.", StringComparison.OrdinalIgnoreCase))
            return new(ApplicationErrorCode.Unauthorized, ErrorKind.Unauthorized, text);
        if (text.Equals("Invalid webhook signature.", StringComparison.OrdinalIgnoreCase))
            return new(ApplicationErrorCode.InvalidWebhookSignature, ErrorKind.Unauthorized, text);
        if (text.Equals("Payout already paid. Manual recovery required.", StringComparison.OrdinalIgnoreCase))
            return new(ApplicationErrorCode.Conflict, ErrorKind.Conflict, text);
        if (preferredCode is ApplicationErrorCode code)
        {
            var kind = code switch
            {
                ApplicationErrorCode.ResourceNotFound => ErrorKind.NotFound,
                ApplicationErrorCode.Forbidden => ErrorKind.Forbidden,
                ApplicationErrorCode.Unauthorized or ApplicationErrorCode.InvalidWebhookSignature => ErrorKind.Unauthorized,
                ApplicationErrorCode.Conflict or ApplicationErrorCode.PaymentAlreadyPaid or ApplicationErrorCode.ConcurrencyConflict => ErrorKind.Conflict,
                _ => ErrorKind.Validation
            };
            return new(code, kind, text);
        }

        if (text.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return new(ApplicationErrorCode.ResourceNotFound, ErrorKind.NotFound, text);

        return new(ApplicationErrorCode.Validation, ErrorKind.Validation, text);
    }
}
