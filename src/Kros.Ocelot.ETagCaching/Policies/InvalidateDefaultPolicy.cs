using Microsoft.AspNetCore.Http;

namespace Kros.Ocelot.ETagCaching.Policies;

internal class InvalidateDefaultPolicy(string[] tagTemplates) : IInvalidateCachePolicy
{
    private readonly string[] _tagTemplates = tagTemplates;

    public ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken)
    {
        if (IsModifyMethod(context.DownstreamRequest.Method))
        {
            context.Tags.UnionWith(TagsHelper.GetTags(_tagTemplates, context.TemplatePlaceholderNameAndValues));
            context.AllowCacheInvalidation = true;
        }
        return ValueTask.CompletedTask;
    }

    private static bool IsModifyMethod(string method)
        => method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Patch || method == HttpMethods.Delete;
}
