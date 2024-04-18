using System.Text;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class TagTemplatesPolicy(string[] tagTemplates) : IETagCachePolicy
{
    private readonly string[] _tagTemplates = tagTemplates;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.Tags.UnionWith(TagsHelper.GetTags(_tagTemplates, context.TemplatePlaceholderNameAndValues));

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
