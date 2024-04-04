using System.Text;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class TagTemplatesPolicy(string[] tagTemplates) : IETagCachePolicy
{
    private readonly string[] _tagTemplates = tagTemplates;

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        var tagSb = new StringBuilder();
        foreach (var tagTemplate in _tagTemplates)
        {
            tagSb.Append(tagTemplate);
            foreach (var template in context.TemplatePlaceholderNameAndValues)
            {
                tagSb.Replace(template.Name, template.Value);
            }
            context.Tags.Add(tagSb.ToString());
            tagSb.Length = 0;
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
