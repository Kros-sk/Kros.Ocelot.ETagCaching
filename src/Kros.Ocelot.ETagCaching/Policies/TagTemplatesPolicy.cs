using System.Text;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class TagTemplatesPolicy(string[] tagTemplates) : IETagCachePolicy
{
    private readonly string[] _tagTemplates = tagTemplates;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        foreach (var tagTemplate in _tagTemplates)
        {
            var tag = new StringBuilder(tagTemplate);
            foreach (var template in context.TemplatePlaceholderNameAndValues)
            {
                tag.Replace(template.Name, template.Value);
            }
            context.Tags.Add(tag.ToString());
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
