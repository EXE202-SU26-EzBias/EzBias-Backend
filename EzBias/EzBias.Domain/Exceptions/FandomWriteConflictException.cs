namespace EzBias.Domain.Exceptions;

public sealed class FandomWriteConflictException : Exception
{
    public FandomWriteConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
