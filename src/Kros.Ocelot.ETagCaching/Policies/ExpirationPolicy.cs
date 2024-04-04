namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class ExpirationPolicy(TimeSpan expiration) : IETagCachePolicy
{
    private readonly TimeSpan _expiration = expiration;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.ETagExpirationTimeSpan = _expiration;
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
}
