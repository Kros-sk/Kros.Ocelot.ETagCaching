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
    public static void AddETagCaching(this OcelotPipelineConfiguration pipelineConfiguration)
    {
        pipelineConfiguration.PreQueryStringBuilderMiddleware = async (ctx, next) =>
        {
            var eTagCachingMiddleware = ctx.RequestServices.GetRequiredService<IETagCachingMiddleware>();
            await eTagCachingMiddleware.InvokeAsync(ctx, next);
        };
    }
}
