using System.Net;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class StatusCodePolicy(int statusCode) : IETagCachePolicy
{
    private readonly int _statusCode = statusCode;

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.StatusCode = (HttpStatusCode)_statusCode;
        return ValueTask.CompletedTask;
    }

    // Stryker disable Block: results in an equivalent mutation
    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
