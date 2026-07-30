namespace EzBias.Application.Common.Results;

public sealed record ApplicationError(
    ApplicationErrorCode Code,
    ErrorKind Kind,
    string Message);
