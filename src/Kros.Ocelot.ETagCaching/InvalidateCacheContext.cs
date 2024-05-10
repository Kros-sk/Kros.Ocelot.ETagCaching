using Microsoft.AspNetCore.Http.Features;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Context for invalidating cache.
/// </summary>
public sealed class InvalidateCacheContext
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
    /// Tags for cache invalidation.
    /// </summary>
    public HashSet<string> Tags { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether cache invalidation is allowed.
    /// </summary>
    public bool AllowCacheInvalidation { get; set; }
}
