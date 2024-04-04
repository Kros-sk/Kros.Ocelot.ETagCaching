using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Net;
using System.Text;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Context for ETag cache.
/// </summary>
public sealed class ETagCacheContext
{
    /// <summary>
    /// HTTP requests features.
    /// </summary>
    public required IFeatureCollection RequestFeatures { get; init; }

    /// <summary>
    /// DI container services.
    /// </summary>
    public required IServiceProvider RequestServices { get; init; }

    /// <summary>
    /// Downstream route template placeholder names and values (from current request).
    /// </summary>
    public required List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; init; }

    /// <summary>
    /// Gets the Ocelot downstream request.
    /// </summary>
    public required DownstreamRequest DownstreamRequest { get; init; }

    /// <summary>
    /// Gets the Ocelot downstream response.
    /// </summary>
    public DownstreamResponse? DownstreamResponse { get; set; }

    /// <summary>
    /// Determines whether the etag caching logic should be configured for the incoming HTTP request.
    /// </summary>
    public bool EnableETagCache { get; set; }

    /// <summary>
    /// Determines whether the response of the HTTP request should be cached.
    /// </summary>
    public bool AllowCacheResponseETag { get; set; }

    /// <summary>
    /// Determines whether the response of the HTTP request should be served from the cache.
    /// </summary>
    public bool AllowNotModified { get; set; }

    /// <summary>
    /// Gets the tag templates of the cached response.
    /// </summary>
    public HashSet<string> Tags { get; } = [];

    /// <summary>
    /// Gets or sets the amount of time the response etag should be cached for.
    /// </summary>
    public TimeSpan ETagExpirationTimeSpan { get; set; }

    /// <summary>
    /// Extra properties to be stored in the cache entry.
    /// </summary>
    public Dictionary<string, object?> CacheEntryExtraProps { get; } = [];

    /// <summary>
    /// Headers which should be added to the response when it is served from the downstream service (not from cache).
    /// </summary>
    public HeaderDictionary ResponseHeaders { get; } = [];

    /// <summary>
    /// Headers which should be added to the response when it is served from the cache.
    /// </summary>
    public HeaderDictionary CachedResponseHeaders { get; } = [];

    /// <summary>
    /// Status code of the response when it is served from the cache.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// ETag value.
    /// </summary>
    public EntityTagHeaderValue ETag { get; set; } = default!;

    internal ETagCacheEntry CacheEntry { get; set; } = default!;

    internal string CacheKey { get; set; } = default!;

    /// <summary>
    /// To string.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(nameof(ETagCacheContext));
        sb.AppendLine("{");
        sb.AppendFormat("\t{0}: {1}", nameof(ETag), ETag);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: \"{1}\"", nameof(CacheKey), CacheKey);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: {1}", nameof(EnableETagCache), EnableETagCache);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: {1}", nameof(AllowNotModified), AllowNotModified);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: {1}", nameof(AllowCacheResponseETag), AllowCacheResponseETag);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: \"{1}\"", nameof(ETagExpirationTimeSpan), ETagExpirationTimeSpan);
        sb.AppendLine();
        sb.AppendFormat("\t{0}: [{1}]", nameof(Tags), string.Join(",", Tags));
        sb.AppendLine();
        sb.AppendFormat("\t{0}: {1}", nameof(StatusCode), StatusCode);
        sb.AppendLine();
        sb.AppendLine("}");

        return sb.ToString();
    }
}
