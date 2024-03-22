using Microsoft.AspNetCore.Http;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Middleware for ETag caching.
/// </summary>
public interface IETagCachingMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">Context of the request.</param>
    /// <param name="next">Next delegate.</param>
    Task InvokeAsync(HttpContext context, Func<Task> next);
}
