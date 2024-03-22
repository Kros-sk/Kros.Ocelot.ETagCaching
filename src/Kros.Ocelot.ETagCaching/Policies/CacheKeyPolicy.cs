using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CacheKeyPolicy(Func<DownstreamRequest, string> keyGenerator) : IETagCachePolicy
{
    private readonly Func<DownstreamRequest, string> _keyGenerator = keyGenerator;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.CacheKey = _keyGenerator(context.DownstreamRequest);
        return ValueTask.CompletedTask;
    }

    // Stryker disable block
    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
