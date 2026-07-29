using System;
using System.Net;

namespace Cliq.VisualStudio.Core.Api
{
    public class CliqApiException : Exception
    {
        public CliqApiException(HttpStatusCode statusCode, CliqProblemDetails problem)
            : base(problem.Detail ?? problem.Title ?? $"Cliq returned HTTP {(int)statusCode}.")
        {
            StatusCode = statusCode;
            Problem = problem;
        }

        public HttpStatusCode StatusCode { get; }
        public CliqProblemDetails Problem { get; }
    }

    public sealed class WorkspaceInUseApiException : CliqApiException
    {
        public WorkspaceInUseApiException(HttpStatusCode statusCode, CliqProblemDetails problem)
            : base(statusCode, problem)
        {
        }

        public WorkspaceLeaseOwner? Owner => Problem.Owner;
    }
}
