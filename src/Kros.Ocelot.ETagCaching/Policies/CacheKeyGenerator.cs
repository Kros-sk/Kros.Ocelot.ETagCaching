using Microsoft.AspNetCore.Http;
using Ocelot.Request.Middleware;
using System.Text;

namespace Kros.Ocelot.ETagCaching.Policies;

/// <summary>
/// Utility class for generating cache keys from different request types.
/// </summary>
internal static class CacheKeyGenerator
{
    /// <summary>
    /// Creates a cache key from downstream request (after Ocelot transformation).
    /// </summary>
    /// <param name="request">The downstream request.</param>
    /// <returns>Generated cache key.</returns>
    public static string CreateFromDownstreamRequest(DownstreamRequest request) =>
        CreateCacheKey(request.Method, request.Scheme, request.Host, request.AbsolutePath, request.Query);

    /// <summary>
    /// Creates a cache key from upstream HTTP request (before Ocelot transformation).
    /// </summary>
    /// <param name="request">The upstream HTTP request.</param>
    /// <returns>Generated cache key.</returns>
    public static string CreateFromUpstreamRequest(HttpRequest request) =>
        CreateCacheKey(request.Method, request.Scheme, request.Host.ToString(),
            request.Path.ToString(), request.QueryString.ToString());

    /// <summary>
    /// Creates a cache key from individual request components.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="scheme">Request scheme (http/https).</param>
    /// <param name="host">Request host.</param>
    /// <param name="path">Request path.</param>
    /// <param name="query">Request query string.</param>
    /// <returns>Generated cache key in format: method:scheme:host:path:query</returns>
    private static string CreateCacheKey(string method, string scheme, string host, string path, string query)
    {
        const char delimiter = ':';
        var sb = new StringBuilder();
        sb.Append(method.ToLower());
        sb.Append(delimiter);
        sb.Append(scheme.ToLower());
        sb.Append(delimiter);
        sb.Append(host.ToLower());
        sb.Append(delimiter);
        sb.Append(path.ToLower());
        sb.Append(delimiter);
        sb.Append(query.ToLower());

        return sb.ToString();
    }
}
