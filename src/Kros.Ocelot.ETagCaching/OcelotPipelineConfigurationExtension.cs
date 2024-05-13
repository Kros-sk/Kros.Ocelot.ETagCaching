using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Middleware;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Extension for <see cref="OcelotPipelineConfiguration"/>.
/// </summary>
public static class OcelotPipelineConfigurationExtension
{
    /// <summary>
    /// Adds ETag caching to the Ocelot pipeline.
    /// </summary>
    /// <param name="pipelineConfiguration">Ocelot pipeline configuration.</param>
    /// <param name="preview">
    /// Preview action which is called before ETag caching.
    /// Use this action instead of <see cref="OcelotPipelineConfiguration.PreQueryStringBuilderMiddleware"/>.
    /// </param>
    public static void AddETagCaching(
        this OcelotPipelineConfiguration pipelineConfiguration,
        Func<HttpContext, Func<Task>, Task>? preview = null)
    {
        pipelineConfiguration.PreQueryStringBuilderMiddleware = async (ctx, next) =>
        {
            preview ??= (_, n) => n();
            await preview(ctx, async () => await ETagCaching(ctx, next));
        };
    }

    private static async Task ETagCaching(HttpContext ctx, Func<Task> next)
    {
        var eTagCachingMiddleware = ctx.RequestServices.GetRequiredService<IETagCachingMiddleware>();
        await eTagCachingMiddleware.InvokeAsync(ctx, next);
    }
}
