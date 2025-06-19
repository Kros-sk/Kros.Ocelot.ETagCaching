using Microsoft.AspNetCore.Http;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CacheKeyPolicy : IETagCachePolicy
{
    private readonly Func<ETagCacheContext, string> _keyGenerator;

    public CacheKeyPolicy(Func<DownstreamRequest, string> keyGenerator)
    {
        _keyGenerator = context => keyGenerator(context.DownstreamRequest);
    }

    private CacheKeyPolicy(Func<ETagCacheContext, string> keyGenerator)
    {
        _keyGenerator = keyGenerator;
    }

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.CacheKey = _keyGenerator(context);
        return ValueTask.CompletedTask;
    }

    // Stryker disable Block: results in an equivalent mutation
    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a cache key policy based on upstream HTTP request (before Ocelot transformation).
    /// </summary>
    /// <param name="keyGenerator">Custom key generator function.</param>
    internal static CacheKeyPolicy FromUpstreamRequest(Func<HttpRequest, string> keyGenerator)
    {
        return new CacheKeyPolicy(context => keyGenerator(context.HttpContext.Request));
    }

    /// <summary>
    /// Creates a cache key policy with default upstream request key generation.
    /// </summary>
    internal static CacheKeyPolicy FromUpstreamRequest()
    {
        return FromUpstreamRequest(CacheKeyGenerator.CreateFromUpstreamRequest);
    }
}
