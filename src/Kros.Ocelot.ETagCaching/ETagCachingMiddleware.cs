using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using Ocelot.Middleware;
using System.Diagnostics.CodeAnalysis;

namespace Kros.Ocelot.ETagCaching;

// routeOptions can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be merged
internal class ETagCachingMiddleware(
    IOptions<List<FakeDownstreamRoute>> routeOptions,
    IOutputCacheStore cacheStore,
    IOptions<ETagCachingOptions> cachingOptions) : IETagCachingMiddleware
{
    private readonly Dictionary<string, FakeDownstreamRoute> _routeOptions = routeOptions.Value.ToDictionary(r => r.Key, r => r);
    private readonly IOutputCacheStore _cacheStore = cacheStore;
    private readonly IOptions<ETagCachingOptions> _cachingOptions = cachingOptions;

    public Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        if (TryGetPolicy(context, out var policy))
        {
            return InvokeCaching(context, policy, next);
        }

        return next();
    }

    private bool TryGetPolicy(HttpContext context, [NotNullWhen(true)] out IETagCachePolicy? policy)
    {
        var downstreamRoute = context.Items.DownstreamRoute();

        if (!string.IsNullOrWhiteSpace(downstreamRoute.Key) && _routeOptions.TryGetValue(downstreamRoute.Key, out var route))
        {
            policy = _cachingOptions.Value.GetPolicy(route.CachePolicy);
            return true;
        }

        policy = null;
        return false;
    }

    private async Task InvokeCaching(HttpContext context, IETagCachePolicy policy, Func<Task> next)
    {
        var cacheContext = CreateContext(context);
        context.Features.Set(new ETagCacheFeature(cacheContext));

        await policy.CacheETagAsync(cacheContext, context.RequestAborted);

        if (cacheContext.EnableETagCache)
        {
            if (cacheContext.AllowNotModified)
            {
                var cacheEntryBytes = await _cacheStore.GetAsync(cacheContext.CacheKey, context.RequestAborted);
                if (cacheEntryBytes != null)
                {
                    var cacheEntry = ETagCacheEntry.Deserialize(cacheEntryBytes);
                    if (cacheEntry is not null && cacheEntry.ETag == cacheContext.ETag.ToString())
                    {
                        cacheContext.CacheEntry = cacheEntry;
                        await policy.ServeNotModifiedAsync(cacheContext, context.RequestAborted);
                        CreateNotModifyResponse(context, cacheContext);

                        return;
                    }
                }
            }

            if (cacheContext.AllowCacheResponseETag)
            {
                await next();
                await CacheDownstreamResponse(context, policy, cacheContext);
                return;
            }
        }

        await next();
        return;
    }

    private async Task CacheDownstreamResponse(HttpContext context, IETagCachePolicy policy, ETagCacheContext cacheContext)
    {
        cacheContext.DownstreamResponse = context.Items.DownstreamResponse();
        await policy.ServeDownstreamResponseAsync(cacheContext, context.RequestAborted);

        if (cacheContext.AllowCacheResponseETag)
        {
            var cacheEntry = new ETagCacheEntry(cacheContext.ETag, cacheContext.CacheEntryExtraProps);

            await _cacheStore.SetAsync(
                cacheContext.CacheKey,
                cacheEntry.Serialize(),
                [.. cacheContext.Tags],
                cacheContext.ETagExpirationTimeSpan,
                context.RequestAborted);

            var response = context.Items.DownstreamResponse();
            foreach (var header in cacheContext.ResponseHeaders)
            {
                response.Headers.Add(new(header.Key, header.Value));
            }
        }
    }

    private static ETagCacheContext CreateContext(HttpContext context) => new ETagCacheContext()
    {
        DownstreamRequest = context.Items.DownstreamRequest(),
        RequestFeatures = context.Features,
        RequestServices = context.RequestServices,
        TemplatePlaceholderNameAndValues = context.Items.TemplatePlaceholderNameAndValues()
    };

    private static void CreateNotModifyResponse(HttpContext context, ETagCacheContext cacheContext)
    {
        StringContent stringContent = new("");
        List<Header> headers = [];

        foreach (var header in cacheContext.CachedResponseHeaders)
        {
            headers.Add(new Header(header.Key, header.Value));
        }

        var response = new DownstreamResponse(stringContent, cacheContext.StatusCode, headers, string.Empty);
        context.Items.UpsertDownstreamResponse(response);
    }
}
