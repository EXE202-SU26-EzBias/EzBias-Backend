namespace EzBias.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public ApplicationError? Failure { get; }
    public bool Success => IsSuccess;
    public string? Error => Failure?.Message;

    protected Result(bool isSuccess, ApplicationError? error)
    {
        IsSuccess = isSuccess;
        Failure = error;
    }

    public static Result Ok() => new(true, null);

    public static Result Fail(ApplicationError error) => new(false, error);

    public static Result Fail(string message, ApplicationErrorCode code = ApplicationErrorCode.Validation)
        => Fail(ApplicationErrorCatalog.FromMessage(message, code));

    public static implicit operator Result((bool Success, string? Error) legacy)
        => legacy.Success
            ? Ok()
            : Fail(ApplicationErrorCatalog.FromMessage(legacy.Error));

    public void Deconstruct(out bool success, out string? error)
    {
        success = IsSuccess;
        error = Error;
    }
}

public sealed class Result<T> : Result
{
    public T? Value { get; }
    public T? Data => Value;

    private Result(bool isSuccess, T? value, ApplicationError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);

    public new static Result<T> Fail(ApplicationError error) => new(false, default, error);

    public new static Result<T> Fail(string message, ApplicationErrorCode code = ApplicationErrorCode.Validation)
        => Fail(ApplicationErrorCatalog.FromMessage(message, code));

    public static implicit operator Result<T>((bool Success, string? Error, T? Data) legacy)
        => legacy.Success && legacy.Data is not null
            ? Ok(legacy.Data)
            : Fail(ApplicationErrorCatalog.FromMessage(legacy.Error));

    public void Deconstruct(out bool success, out string? error, out T? data)
    {
        success = IsSuccess;
        error = Error;
        data = Value;
    }
}
