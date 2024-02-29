namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class EnableCachePolicy : IETagCachePolicy
{
    private EnableCachePolicy()
    {
    }

    public static EnableCachePolicy Enabled { get; } = new EnableCachePolicy();

    public static EnableCachePolicy Disabled { get; } = new EnableCachePolicy();

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.EnableETagCache = this == Enabled;
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
