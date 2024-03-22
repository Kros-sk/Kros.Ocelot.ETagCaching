using Microsoft.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CacheControlPolicy(CacheControlHeaderValue cacheControl) : IETagCachePolicy
{
    private readonly CacheControlHeaderValue _cacheControl = cacheControl;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        // Stryker disable Block: results in an equivalent mutation
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.ResponseHeaders[HeaderNames.CacheControl] = _cacheControl.ToString();

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.ResponseHeaders[HeaderNames.CacheControl] = _cacheControl.ToString();

        return ValueTask.CompletedTask;
    }
}
