using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class DefaultPolicyShould
{
    [Fact]
    public async Task EnableETagCache()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);

        context.EnableETagCache.Should().BeTrue();
    }

    [Fact]
    public async Task DisableCacheIfNoCacheHeaderIsPresent()
    {
        var context = ETagCacheContextFactory.CreateContext();
        context.DownstreamRequest.Headers.Add("Cache-Control", "no-cache");
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);

        context.EnableETagCache.Should().BeFalse();
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
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);

        context.AllowCacheResponseETag.Should().Be(allowed);
    }

    [Fact]
    public async Task AllowCacheResponseFor200StatusCode()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);
        await policy.ServeDownstreamResponseAsync(context, default);

        context.AllowCacheResponseETag.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(Non200StatusCodes))]
    public async Task DoNotAllowCacheResponseForNon200StatusCode(HttpStatusCode statusCode)
    {
        var context = ETagCacheContextFactory.CreateContext(statusCode: statusCode);
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);
        await policy.ServeDownstreamResponseAsync(context, default);

        context.AllowCacheResponseETag.Should().BeFalse();
    }

    [Fact]
    public async Task DoNotAllowCacheResponseForNoStoreHeader()
    {
        var context = ETagCacheContextFactory.CreateContext();
        context.DownstreamRequest.Headers.Add("Cache-Control", "no-store");
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);
        await policy.ServeDownstreamResponseAsync(context, default);

        context.AllowCacheResponseETag.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultExpirationTime()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);

        context.ETagExpirationTimeSpan.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task SetCacheKey()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.CacheETagAsync(context, default);

        context.CacheKey.Should().Be("get:http:localhost:/api/2/products:?skip=10&take=5");
    }

    [Fact]
    public async Task SetStatusCodeNotModifiedIfServeFromCache()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.ServeNotModifiedAsync(context, default);

        context.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact]
    public async Task SetResponseHeadersWhenCacheValue()
    {
        var context = ETagCacheContextFactory.CreateContext();
        var policy = new DefaultPolicy();

        await policy.ServeDownstreamResponseAsync(context, default);

        context.ResponseHeaders.Should().Contain("Cache-Control", "private");
        context.ResponseHeaders.Should().ContainKey("ETag");
        context.ResponseHeaders["ETag"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetCachedResponseHeadersWhenCacheValue()
    {
        var context = ETagCacheContextFactory.CreateContext(etagValue: "incommingetag");
        var policy = new DefaultPolicy();

        await policy.ServeNotModifiedAsync(context, default);

        context.CachedResponseHeaders.Should().Contain("Cache-Control", "private");
        context.CachedResponseHeaders.Should().Contain("ETag", context.ETag.ToString());
    }

    public static TheoryData<HttpStatusCode> Non200StatusCodes()
    {
        var codes = Enum.GetValues(typeof(HttpStatusCode));
        var data = new TheoryData<HttpStatusCode>();
        foreach (HttpStatusCode code in codes.Cast<HttpStatusCode>())
        {
            if (code != HttpStatusCode.OK)
            {
                data.Add(code);
            }
        }

        return data;
    }
}
