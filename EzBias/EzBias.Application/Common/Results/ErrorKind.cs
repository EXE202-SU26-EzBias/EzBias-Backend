namespace EzBias.Application.Common.Results;

public enum ErrorKind
{
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Unauthorized = 4,
    Conflict = 5
}
