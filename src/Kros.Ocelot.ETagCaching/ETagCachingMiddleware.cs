﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Ocelot.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching;

// routeOptions can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be merged
internal partial class ETagCachingMiddleware(
    IOptions<List<FakeDownstreamRoute>> routeOptions,
    IOutputCacheStore cacheStore,
    IOptions<ETagCachingOptions> cachingOptions,
    ILogger<ETagCachingMiddleware> logger) : IETagCachingMiddleware
{
    private readonly Dictionary<string, FakeDownstreamRoute> _routeOptions = routeOptions.Value.ToDictionary(r => r.Key, r => r);
    private readonly IOutputCacheStore _cacheStore = cacheStore;
    private readonly IOptions<ETagCachingOptions> _cachingOptions = cachingOptions;
    private readonly ILogger<ETagCachingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        LogMiddlewareStart(context.Request.GetDisplayUrl());

        if (TryGetTagsForInvalidation(context, out var invalidatePolicy))
        {
            await InvalidateCache(context, invalidatePolicy);
        }

        if (TryGetPolicy(context, out var policy))
        {
            await InvokeCaching(context, policy, next);
            return;
        }

        await next();
    }

    private async Task InvalidateCache(
        HttpContext context,
        IInvalidateCachePolicy invalidatePolicy)
    {
        var invalidateContext = new InvalidateCacheContext()
        {
            DownstreamRequest = context.Items.DownstreamRequest(),
            RequestFeatures = context.Features,
            RequestServices = context.RequestServices,
            TemplatePlaceholderNameAndValues = context.Items.TemplatePlaceholderNameAndValues()
        };

        await invalidatePolicy.InvalidateCacheAsync(invalidateContext, context.RequestAborted);

        if (invalidateContext.AllowCacheInvalidation)
        {
            foreach (var tag in invalidateContext.Tags)
            {
                LogCacheEviction(tag);
                await _cacheStore.EvictByTagAsync(tag, context.RequestAborted);
            }
        }
    }

    private bool TryGetPolicy(HttpContext context, [NotNullWhen(true)] out IETagCachePolicy? policy)
    {
        var downstreamRoute = context.Items.DownstreamRoute();

        if (!string.IsNullOrWhiteSpace(downstreamRoute.Key)
            && _routeOptions.TryGetValue(downstreamRoute.Key, out var route)
            && !string.IsNullOrEmpty(route.CachePolicy))
        {
            policy = _cachingOptions.Value.GetPolicy(route.CachePolicy);
            return true;
        }

        policy = null;
        return false;
    }

    private async Task InvokeCaching(HttpContext context, IETagCachePolicy policy, Func<Task> next)
    {
        LogInvokeCaching();
        var cacheContext = CreateContext(context);
        context.Features.Set(new ETagCacheFeature(cacheContext));

        await policy.CacheETagAsync(cacheContext, context.RequestAborted);

        LogETagCacheContext(cacheContext);

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

                        LogServeNotModified(cacheContext);

                        return;
                    }
                }
            }

            if (cacheContext.AllowCacheResponseETag)
            {
                LogGetResourceFromDownstream(cacheContext.DownstreamRequest.ToUri());

                await next();
                await CacheDownstreamResponse(context, policy, cacheContext);
                return;
            }
        }
        else
        {
            LogETagCachingDisabled(
                context.Request.GetDisplayUrl(),
                cacheContext.ETag?.ToString(),
                context.Request?.Headers.CacheControl,
                GetHeader(cacheContext.DownstreamRequest.Headers, HeaderNames.CacheControl),
                GetHeader(cacheContext.DownstreamRequest.Headers, HeaderNames.IfNoneMatch));
        }

        await next();
        return;
    }

    private static string? GetHeader(HttpHeaders header, string headerName)
    {
        if (header.TryGetValues(headerName, out var values))
        {
            return string.Join(", ", values);
        }

        return string.Empty;
    }

    private async Task CacheDownstreamResponse(HttpContext context, IETagCachePolicy policy, ETagCacheContext cacheContext)
    {
        cacheContext.DownstreamResponse = context.Items.DownstreamResponse();
        await policy.ServeDownstreamResponseAsync(cacheContext, context.RequestAborted);

        if (cacheContext.AllowCacheResponseETag)
        {
            var cacheEntry = new ETagCacheEntry(cacheContext.ETag, cacheContext.CacheEntryExtraProps);

            LogSettingToCacheStore(cacheContext);

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
        HttpContext = context,
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

    private bool TryGetTagsForInvalidation(HttpContext context, [NotNullWhen(true)] out IInvalidateCachePolicy? policy)
    {
        var downstreamRoute = context.Items.DownstreamRoute();

        if (!string.IsNullOrWhiteSpace(downstreamRoute.Key)
            && _routeOptions.TryGetValue(downstreamRoute.Key, out var route)
            && !string.IsNullOrEmpty(route.InvalidateCachePolicy))
        {
            policy = _cachingOptions.Value.GetInvalidatePolicy(route.InvalidateCachePolicy);
            return true;
        }

        policy = null;
        return false;
    }

    [LoggerMessage("ETagCachingMiddleware start for the request: '{requestPath}'", Level = LogLevel.Debug)]
    partial void LogMiddlewareStart(string requestPath);

    [LoggerMessage("Invoking caching.", Level = LogLevel.Debug)]
    partial void LogInvokeCaching();

    [LoggerMessage("{cacheContext}", Level = LogLevel.Information)]
    partial void LogETagCacheContext(ETagCacheContext cacheContext);

    [LoggerMessage("Serve not modified: {cacheContext}", Level = LogLevel.Information)]
    partial void LogServeNotModified(ETagCacheContext cacheContext);

    [LoggerMessage("Get resource from downstream service: {downstreamPath}", Level = LogLevel.Information)]
    partial void LogGetResourceFromDownstream(string downstreamPath);

    [LoggerMessage("Setting information to cache store: {cacheContext}.", Level = LogLevel.Information)]
    partial void LogSettingToCacheStore(ETagCacheContext cacheContext);

    [LoggerMessage("Cache eviction for tag: {tag}", Level = LogLevel.Information)]
    partial void LogCacheEviction(string tag);

    [LoggerMessage("ETag caching is disabled for request: '{requestPath}'. ETag: {eTag}, " +
        "Upstream Cache-control: {upstreamCacheControl}, Downstream Cache-control: {downstreamCacheControl}, " +
        "If-None-Match: {ifNoneMatch}", Level = LogLevel.Information)]
    partial void LogETagCachingDisabled(
        string? requestPath,
        string? eTag,
        string? upstreamCacheControl,
        string? downstreamCacheControl,
        string? ifNoneMatch);
}
