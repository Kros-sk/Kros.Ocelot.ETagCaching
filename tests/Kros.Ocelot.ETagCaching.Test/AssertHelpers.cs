using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Kros.Ocelot.ETagCaching.Test;

internal static class AssertHelpers
{
    public static void AssertContextEqual(
        ETagCacheContext expected,
        ETagCacheContext actual,
        bool excludeResponseHeaders = false,
        bool excludeETag = false)
    {
        Assert.Equal(expected.EnableETagCache, actual.EnableETagCache);
        Assert.Equal(expected.AllowCacheResponseETag, actual.AllowCacheResponseETag);
        Assert.Equal(expected.AllowNotModified, actual.AllowNotModified);
        Assert.Equal(expected.ETagExpirationTimeSpan, actual.ETagExpirationTimeSpan);
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Tags, actual.Tags);

        AssertDictionaryEquivalent(expected.CacheEntryExtraProps, actual.CacheEntryExtraProps);

        if (!excludeResponseHeaders)
        {
            AssertHeaderDictionaryEquivalent(expected.ResponseHeaders, actual.ResponseHeaders);
        }

        AssertHeaderDictionaryEquivalent(expected.CachedResponseHeaders, actual.CachedResponseHeaders);

        if (!excludeETag)
        {
            Assert.Equal(expected.ETag, actual.ETag);
        }

        Assert.Equal(GetCacheKey(expected), GetCacheKey(actual));
    }

    public static void AssertHeaderContains(HeaderDictionary headers, string key, string value)
    {
        Assert.True(headers.TryGetValue(key, out var values));
        Assert.Contains<string>(value, values);
    }

    public static void AssertDictionaryEquivalent<TKey, TValue>(
        IDictionary<TKey, TValue> expected,
        IDictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (key, value) in expected)
        {
            Assert.True(actual.TryGetValue(key, out var actualValue));
            Assert.Equal(value, actualValue);
        }
    }

    private static void AssertHeaderDictionaryEquivalent(HeaderDictionary expected, HeaderDictionary actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var key in expected.Keys)
        {
            Assert.True(actual.TryGetValue(key, out var actualValues));
            Assert.Equal(expected[key], actualValues);
        }
    }

    private static string? GetCacheKey(ETagCacheContext context)
    {
        var property = typeof(ETagCacheContext).GetProperty("CacheKey", BindingFlags.Instance | BindingFlags.NonPublic);
        return property?.GetValue(context) as string;
    }
}
