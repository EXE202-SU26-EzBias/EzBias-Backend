namespace EzBias.Application.Common.Results;

/// <summary>
/// Compatibility bridge while feature interfaces are migrated in slices. It keeps
/// existing success payloads and messages intact while exposing typed errors to API code.
/// </summary>
public static class LegacyResultExtensions
{
    public static Result<T> ToResult<T>(this Result<T> result) => result;

    public static Result ToResult(this Result result) => result;

    public static Result<T> ToResult<T>(this (bool Success, string? Error, T? Data) result)
        => result.Success && result.Data is not null
            ? Result<T>.Ok(result.Data)
            : Result<T>.Fail(ApplicationErrorCatalog.FromMessage(result.Error));

    public static Result ToResult(this (bool Success, string? Error) result)
        => result.Success
            ? Result.Ok()
            : Result.Fail(ApplicationErrorCatalog.FromMessage(result.Error));
}
