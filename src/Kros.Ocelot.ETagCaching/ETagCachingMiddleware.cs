using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kros.Ocelot.ETagCaching;

// routeOptions can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be merged
internal class ETagCachingMiddleware(
    IOptions<IEnumerable<DownstreamRoute>> routeOptions,
    IOutputCacheStore cacheStore,
    ETagCachingOptions cachingOptions)
{
    private readonly IOptions<IEnumerable<DownstreamRoute>> _routeOptions = routeOptions;
    private readonly IOutputCacheStore _cacheStore = cacheStore;
    private readonly ETagCachingOptions _cachingOptions = cachingOptions;

    public async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        await next();
    }

    private bool TryGetPolicy(HttpContext context, out IETagCachePolicy? policy)
    {
        policy = null;
        return false;
    }
}
