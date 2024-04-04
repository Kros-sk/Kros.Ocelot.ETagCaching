namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Interface for ETag cache policy. An implementation of this interface can update how the current request information is cached.
/// </summary>
public interface IETagCachePolicy
{
    /// <summary>
    /// Updates the <see cref="ETagCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    /// <param name="context">The <see cref="ETagCacheContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="ETagCacheContext"/> before the response is served from downstream service and can be cached.
    /// At that point cacheability of the response etag can be updated.
    /// </summary>
    /// <param name="context">The <see cref="ETagCacheContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="ETagCacheContext"/> before not modified response is serve.
    /// At that point the response headers, status code etc. can be updated.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken);
}
