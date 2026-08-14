using System;
using System.Collections.Generic;
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

    public sealed class ScanReviewConflictApiException : CliqApiException
    {
        public ScanReviewConflictApiException(
            HttpStatusCode statusCode,
            CliqProblemDetails problem,
            IReadOnlyList<Cliq.Domain.ScanReviewReference>? references = null)
            : base(statusCode, problem)
        {
            Code = problem.Code ?? string.Empty;
            References = references ?? Array.Empty<Cliq.Domain.ScanReviewReference>();
        }

        public string Code { get; }
        public IReadOnlyList<Cliq.Domain.ScanReviewReference> References { get; }
    }
}

