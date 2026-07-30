namespace EzBias.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public ApplicationError? Failure { get; }

    protected Result(bool isSuccess, ApplicationError? error)
    {
        IsSuccess = isSuccess;
        Failure = error;
    }

    public static Result Ok() => new(true, null);

    public static Result Fail(ApplicationError error) => new(false, error);

    public static Result Fail(string message, ApplicationErrorCode code)
        => Fail(ApplicationError.Create(code, message));
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, ApplicationError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);

    public new static Result<T> Fail(ApplicationError error) => new(false, default, error);

    public new static Result<T> Fail(string message, ApplicationErrorCode code)
        => Fail(ApplicationError.Create(code, message));
}
