using EzBias.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace EzBias.API.Mappings;

public static class ApiResultMapper
{
    public static IActionResult ToErrorActionResult(
        this ControllerBase controller,
        Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Only failed results can be mapped to an error response.");

        var error = result.Failure
            ?? ApplicationError.Create(
                ApplicationErrorCode.Validation,
                "Request could not be completed.");
        return error.Kind switch
        {
            ErrorKind.Forbidden => controller.Forbid(),
            ErrorKind.Unauthorized => controller.Unauthorized(new { message = error.Message }),
            ErrorKind.NotFound => controller.NotFound(new { message = error.Message }),
            ErrorKind.Conflict => controller.Conflict(new { message = error.Message }),
            _ => controller.BadRequest(new { message = error.Message })
        };
    }
}
