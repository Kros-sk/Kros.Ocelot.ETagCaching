using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Request.Middleware;
using System.Diagnostics.CodeAnalysis;

namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class DefaultPolicy : IETagCachePolicy
{
    private DefaultPolicy()
    {
    }

    public static DefaultPolicy Instance { get; } = new();

    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.EnableETagCache = !HaveRequestNoCacheHeader(context);
        context.AllowCacheResponseETag = IsGetMethod(context);
        context.ETagExpirationTimeSpan = TimeSpan.FromSeconds(30);
        context.CacheKey = CreateCacheKey(context.DownstreamRequest);
        if (AllowServeFromCache(context, out var entityTag))
        {
            context.ETag = entityTag;
            context.AllowNotModified = true;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.StatusCode = System.Net.HttpStatusCode.NotModified;
        AddCacheHeaders(context.CachedResponseHeaders, context.ETag.ToString());

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        context.AllowCacheResponseETag = false;

        if (AllowCacheResponseETag(context))
        {
            context.AllowCacheResponseETag = true;
            context.ETag = new EntityTagHeaderValue($"\"{Guid.NewGuid()}\"");
            AddCacheHeaders(context.ResponseHeaders, context.ETag.ToString());
        }

        return ValueTask.CompletedTask;
    }

    private static void AddCacheHeaders(HeaderDictionary headers, string etag)
    {
        headers[HeaderNames.CacheControl] = CacheControlHeaderValue.PrivateString;
        headers[HeaderNames.ETag] = etag;
    }

    private static bool AllowCacheResponseETag(ETagCacheContext context)
    {
        if (!IsGetMethod(context))
        {
            return false;
        }

        if (context.DownstreamResponse is null
            || context.DownstreamResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return false;
        }

        if (HaveCacheHeaderValue(context.DownstreamRequest.Headers, CacheControlHeaderValue.NoStoreString))
        {
            return false;
        }

        return true;
    }

    private static bool HaveRequestNoCacheHeader(ETagCacheContext context)
        => HaveCacheHeaderValue(context.DownstreamRequest.Headers, CacheControlHeaderValue.NoCacheString);

    private static bool HaveCacheHeaderValue(System.Net.Http.Headers.HttpHeaders headers, string value)
        => headers.TryGetValues(HeaderNames.CacheControl, out var values)
            && values.Contains(value);

    private static bool IsGetMethod(ETagCacheContext context)
        => context.DownstreamRequest.Method == HttpMethods.Get;

    private static string CreateCacheKey(DownstreamRequest downstreamRequest) =>
        CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

    private static bool AllowServeFromCache(ETagCacheContext context, [NotNullWhen(true)] out EntityTagHeaderValue? entityTag)
    {
        entityTag = null;
        if (context.DownstreamRequest.Headers.TryGetValues(HeaderNames.IfNoneMatch, out var values)
            && values.Count() == 1)
        {
            return EntityTagHeaderValue.TryParse(values.First(), out entityTag);
        }

        return false;
    }
}
