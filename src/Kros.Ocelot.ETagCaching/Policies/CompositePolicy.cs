namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CompositePolicy(IEnumerable<IETagCachePolicy> eTagCachePolicies) : IETagCachePolicy
{
    private readonly IEnumerable<IETagCachePolicy> _eTagCachePolicies = eTagCachePolicies;

    public async ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var policy in _eTagCachePolicies)
        {
            await policy.CacheETagAsync(context, cancellationToken);
        }
    }

    public async ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var policy in _eTagCachePolicies)
        {
            await policy.ServeNotModifiedAsync(context, cancellationToken);
        }
    }

    public async ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var policy in _eTagCachePolicies)
        {
            await policy.ServeDownstreamResponseAsync(context, cancellationToken);
        }
    }
}
