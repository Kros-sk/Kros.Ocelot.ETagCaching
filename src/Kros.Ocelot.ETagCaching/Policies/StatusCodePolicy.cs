namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class StatusCodePolicy(int statusCode) : IETagCachePolicy
{
    private readonly int _statusCode = statusCode;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.StatusCode = _statusCode;
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
