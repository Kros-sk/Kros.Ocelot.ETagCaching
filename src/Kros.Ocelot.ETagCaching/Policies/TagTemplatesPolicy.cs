namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class TagTemplatesPolicy(string[] tagTemplates) : IETagCachePolicy
{
    private readonly string[] _tagTemplates = tagTemplates;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var tag in _tagTemplates)
        {
            context.TagTemplates.Add(tag);
        }

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
