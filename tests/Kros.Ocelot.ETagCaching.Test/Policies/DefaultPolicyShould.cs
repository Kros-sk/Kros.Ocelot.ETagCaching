using Kros.Ocelot.ETagCaching.Policies;
using System.Net;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class DefaultPolicyShould
{
    [Fact]
    public async Task EnableETagCache()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.EnableETagCache);
    }

    [Fact]
    public async Task DisableCacheIfNoCacheHeaderIsPresent()
    {
        var context = ETagCacheContextFactory.CreateContext();
        context.DownstreamRequest.Headers.Add("Cache-Control", "no-cache");
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.False(context.EnableETagCache);
    }

    [Theory]
    [InlineData("GET", true)]
    [InlineData("POST", false)]
    [InlineData("PUT", false)]
    [InlineData("DELETE", false)]
    [InlineData("PATCH", false)]
    [InlineData("HEAD", false)]
    [InlineData("OPTIONS", false)]
    public async Task AllowCacheResponseForGetMethod(string httpMethod, bool allowed)
    {
        var context = ETagCacheContextFactory.CreateContext(httpMethod);
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(allowed, context.AllowCacheResponseETag);
    }

    [Fact]
    public async Task AllowCacheResponseFor200StatusCode()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);
        await policy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.AllowCacheResponseETag);
    }

    [Theory]
    [MemberData(nameof(Non200StatusCodes))]
    public async Task DoNotAllowCacheResponseForNon200StatusCode(HttpStatusCode statusCode)
    {
        var context = ETagCacheContextFactory.CreateContext(statusCode: statusCode);
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);
        await policy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        Assert.False(context.AllowCacheResponseETag);
    }

    [Fact]
    public async Task DoNotAllowCacheResponseForNoStoreHeader()
    {
        var context = ETagCacheContextFactory.CreateContext();
        context.DownstreamRequest.Headers.Add("Cache-Control", "no-store");
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);
        await policy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        Assert.False(context.AllowCacheResponseETag);
    }

    [Fact]
    public async Task SetDefaultExpirationTime()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromSeconds(30), context.ETagExpirationTimeSpan);
    }

    [Fact]
    public async Task SetCacheKey()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal("get:http:localhost:/api/2/products:?skip=10&take=5", context.CacheKey);
    }

    [Fact]
    public async Task SetStatusCodeNotModifiedIfServeFromCache()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;
        context.DownstreamRequest.Headers.Add("If-None-Match", "\"incommingetag\"");

        await policy.ServeNotModifiedAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, context.StatusCode);
    }

    [Fact]
    public async Task SetResponseHeadersWhenCacheValue()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        AssertHelpers.AssertHeaderContains(context.ResponseHeaders, "Cache-Control", "private");
        Assert.True(context.ResponseHeaders.ContainsKey("ETag"));
        Assert.False(Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(context.ResponseHeaders["ETag"]));
    }

    [Fact]
    public async Task SetCachedResponseHeadersWhenCacheValue()
    {
        var context = ETagCacheContextFactory.CreateContext(etagValue: "incommingetag");
        var policy = DefaultPolicy.Instance;
        context.DownstreamRequest.Headers.Add("If-None-Match", "\"incommingetag\"");

        await policy.ServeNotModifiedAsync(context, TestContext.Current.CancellationToken);

        AssertHelpers.AssertHeaderContains(context.CachedResponseHeaders, "Cache-Control", "private");
        AssertHelpers.AssertHeaderContains(context.CachedResponseHeaders, "ETag", context.ETag.ToString());
    }

    [Fact]
    public async Task DisallowServeFromCacheWhenIfNoneMatchHeaderIsNotPresent()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.False(context.AllowNotModified);
    }

    [Fact]
    public async Task AllowServeFromCacheWhenIfNoneMatchHeaderIsPresent()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = DefaultPolicy.Instance;
        context.DownstreamRequest.Headers.Add("If-None-Match", "\"incommingetag\"");

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.AllowNotModified);
        Assert.Equal("\"incommingetag\"", context.ETag.ToString());
    }

    public static TheoryData<HttpStatusCode> Non200StatusCodes()
    {
        var codes = Enum.GetValues(typeof(HttpStatusCode));
        var data = new TheoryData<HttpStatusCode>();
        foreach (var code in codes.Cast<HttpStatusCode>())
        {
            if (code != HttpStatusCode.OK)
            {
                data.Add(code);
            }
        }

        return data;
    }
}
