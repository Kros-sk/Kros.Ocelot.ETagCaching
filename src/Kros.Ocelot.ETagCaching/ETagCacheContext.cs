using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;

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
    public bool AllowNotModified { get; set; } = true;

    /// <summary>
    /// Gets the tag templates of the cached response.
    /// </summary>
    public HashSet<string> TagTemplates { get; } = [];

    /// <summary>
    /// Gets or sets the amount of time the response etag should be cached for.
    /// </summary>
    public TimeSpan? ETagExpirationTimeSpan { get; set; }

    /// <summary>
    /// Extra properties to be stored in the cache entry.
    /// </summary>
    public Dictionary<string, object?> CacheEntryExtraProps { get; } = [];

    /// <summary>
    /// Headers which should be added to the response when it is served from the downstream service (No from cache).
    /// </summary>
    public HeaderDictionary ResponseHeaders { get; } = [];

    /// <summary>
    /// Headers which should be added to the response when it is served from the cache.
    /// </summary>
    public HeaderDictionary CachedResponseHeaders { get; } = [];

    /// <summary>
    /// Status code of the response when it is served from the cache.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// ETag value.
    /// </summary>
    public EntityTagHeaderValue ETag { get; set; } = default!;

    internal ETagCacheEntry CacheEntry { get; set; } = default!;

    internal string CacheKey { get; set; } = default!;
}
