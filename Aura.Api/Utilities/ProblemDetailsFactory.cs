using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Utilities
{
    /// <summary>
    /// Factory for creating standardized ProblemDetails responses in controllers.
    /// Implements RFC 7807 Problem Details for HTTP APIs with consistent formatting
    /// and correlation ID tracking.
    /// </summary>
    public static class ProblemDetailsFactory
    {
        private const string ErrorTypeBaseUrl = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md";

        /// <summary>
        /// Creates a BadRequest (400) ProblemDetails response
        /// </summary>
        /// <param name="detail">Detailed error message</param>
        /// <param name="correlationId">Correlation ID for tracking (typically from HttpContext.TraceIdentifier)</param>
        /// <param name="field">Optional field name that caused the error</param>
        /// <param name="title">Optional custom title (defaults to "Invalid Request")</param>
        /// <returns>BadRequestObjectResult with ProblemDetails</returns>
        public static IActionResult CreateBadRequest(
            string detail,
            string? correlationId = null,
            string? field = null,
            string? title = null)
        {
            var problemDetails = new
            {
                type = $"{ErrorTypeBaseUrl}#E400",
                title = title ?? "Invalid Request",
                status = 400,
                detail,
                correlationId,
                field
            };

            return new BadRequestObjectResult(problemDetails);
        }

        /// <summary>
        /// Creates a NotFound (404) ProblemDetails response
        /// </summary>
        /// <param name="detail">Detailed error message</param>
        /// <param name="correlationId">Correlation ID for tracking (typically from HttpContext.TraceIdentifier)</param>
        /// <param name="resourceId">Optional resource ID that was not found</param>
        /// <param name="resourceType">Optional resource type (e.g., "Project", "Profile")</param>
        /// <param name="title">Optional custom title (defaults based on resourceType or "Not Found")</param>
        /// <returns>NotFoundObjectResult with ProblemDetails</returns>
        public static IActionResult CreateNotFound(
            string detail,
            string? correlationId = null,
            string? resourceId = null,
            string? resourceType = null,
            string? title = null)
        {
            var effectiveTitle = title ?? (resourceType != null ? $"{resourceType} Not Found" : "Not Found");
            
            var problemDetails = new
            {
                type = $"{ErrorTypeBaseUrl}#E404",
                title = effectiveTitle,
                status = 404,
                detail,
                correlationId,
                projectId = resourceType == "Project" ? resourceId : null,
                profileId = resourceType == "Profile" ? resourceId : null,
                resourceId = (resourceType != "Project" && resourceType != "Profile") ? resourceId : null
            };

            return new NotFoundObjectResult(problemDetails);
        }

        /// <summary>
        /// Creates an InternalServerError (500) ProblemDetails response
        /// </summary>
        /// <param name="detail">Detailed error message</param>
        /// <param name="correlationId">Correlation ID for tracking (typically from HttpContext.TraceIdentifier)</param>
        /// <param name="title">Optional custom title (defaults to "Server Error")</param>
        /// <returns>ObjectResult with status 500 and ProblemDetails</returns>
        public static IActionResult CreateInternalServerError(
            string detail,
            string? correlationId = null,
            string? title = null)
        {
            var problemDetails = new
            {
                type = $"{ErrorTypeBaseUrl}#E500",
                title = title ?? "Server Error",
                status = 500,
                detail,
                correlationId
            };

            return new ObjectResult(problemDetails)
            {
                StatusCode = 500
            };
        }

        /// <summary>
        /// Creates a custom status code ProblemDetails response
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="detail">Detailed error message</param>
        /// <param name="title">Error title</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="errorCode">Optional error code (e.g., "E400", "E404")</param>
        /// <returns>ObjectResult with specified status code and ProblemDetails</returns>
        public static IActionResult CustomError(
            int statusCode,
            string detail,
            string title,
            string? correlationId = null,
            string? errorCode = null)
        {
            var effectiveErrorCode = errorCode ?? $"E{statusCode}";
            
            var problemDetails = new
            {
                type = $"{ErrorTypeBaseUrl}#{effectiveErrorCode}",
                title,
                status = statusCode,
                detail,
                correlationId
            };

            return new ObjectResult(problemDetails)
            {
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Helper method to extract correlation ID from HttpContext
        /// </summary>
        /// <param name="httpContext">The current HttpContext</param>
        /// <returns>Correlation ID (TraceIdentifier)</returns>
        public static string GetCorrelationId(HttpContext httpContext)
        {
            return httpContext.TraceIdentifier;
        }
    }
}
