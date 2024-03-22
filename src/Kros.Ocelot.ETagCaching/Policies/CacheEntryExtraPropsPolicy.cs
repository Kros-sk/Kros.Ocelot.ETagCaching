namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CacheEntryExtraPropsPolicy(IEnumerable<KeyValuePair<string, object>> extraProps)
    : IETagCachePolicy
{
    private readonly IEnumerable<KeyValuePair<string, object>> _extraProps = extraProps;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var prop in _extraProps)
        {
            context.CacheEntryExtraProps[prop.Key] = prop.Value;
        }
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
