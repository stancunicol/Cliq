using Cliq.Application;
using Microsoft.AspNetCore.Mvc;

namespace Cliq.Api;

internal static class ApiErrors
{
    internal static ProblemDetails Create(Exception exception)
    {
        var status = exception switch
        {
            WorkspaceInUseException => StatusCodes.Status409Conflict,
            WorkspaceNotOpenException => StatusCodes.Status409Conflict,
            WorkspaceValidationException or ArgumentException => StatusCodes.Status400BadRequest,
            DirectoryNotFoundException or KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException when exception.Message.Contains("already active", StringComparison.Ordinal) =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status500InternalServerError
                ? "Internal server error"
                : "Request failed",
            Detail = exception.Message,
        };

        if (exception is WorkspaceInUseException workspaceInUse)
        {
            problem.Title = "Workspace is already in use";
            problem.Extensions["code"] = "workspace_in_use";
            if (workspaceInUse.Owner is not null)
            {
                problem.Extensions["owner"] = workspaceInUse.Owner;
            }
        }

        return problem;
    }
}
