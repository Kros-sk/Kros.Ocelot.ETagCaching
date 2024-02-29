using Microsoft.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class ETagPolicy(Func<ETagCacheContext, EntityTagHeaderValue> etagGenerator) : IETagCachePolicy
{
    private readonly Func<ETagCacheContext, EntityTagHeaderValue> _etagGenerator = etagGenerator;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.ETag = _etagGenerator(context);
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
