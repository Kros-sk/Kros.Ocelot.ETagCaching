using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.AspNetCore.Http;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CacheKeyPolicyShould
{
    [Fact]
    public async Task CreateCacheKeyInContext_WhenCacheETagAsyncWasCall()
    {
        var keyGenerator = new Func<DownstreamRequest, string>(d => d.OriginalString.GetHashCode().ToString());
        var policy = new CacheKeyPolicy(keyGenerator);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.CacheETagAsync(context, default);

        context.CacheKey.Should().Be(keyGenerator(context.DownstreamRequest));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheKeyPolicy((_) => string.Empty);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context2, default);
        await extraPropsPolicy.ServeNotModifiedAsync(context2, default);

        context.Should().BeEquivalentTo(context2, o =>
            o.Excluding(p => p.HttpContext));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheKeyPolicy((_) => string.Empty);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, default);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, default);

        context.Should().BeEquivalentTo(context2,
            options => options
                .Excluding(p => p.ResponseHeaders)
                .Excluding(p => p.ETag)
                .Excluding(p => p.HttpContext));
    }

    [Fact]
    public async Task CreateUpstreamCacheKeyInContext_WhenFromUpstreamRequestWithCustomGeneratorWasUsed()
    {
        var keyGenerator = new Func<HttpRequest, string>(r => $"custom:{r.Method}:{r.Path}");
        var policy = CacheKeyPolicy.FromUpstreamRequest(keyGenerator);

        var context = ETagCacheContextFactory.CreateContext(
            upstreamPath: "/api/1/products",
            upstreamQuery: "?filter=active");
        await policy.CacheETagAsync(context, default);

        context.CacheKey.Should().Be("custom:GET:/api/1/products");
    }

    [Fact]
    public async Task CreateDefaultUpstreamCacheKeyInContext_WhenFromUpstreamRequestWithoutGeneratorWasUsed()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var context = ETagCacheContextFactory.CreateContext(
            httpMethod: "POST",
            upstreamPath: "/api/1/orders",
            upstreamQuery: "?include=details");
        await policy.CacheETagAsync(context, default);

        var expectedKey = "post:http:localhost:5000:/api/1/orders:?include=details";
        context.CacheKey.Should().Be(expectedKey);
    }

    [Fact]
    public async Task CreateUpstreamCacheKey_WithEmptyQueryString()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var context = ETagCacheContextFactory.CreateContext(
            upstreamPath: "/api/1/users",
            upstreamQuery: "");
        await policy.CacheETagAsync(context, default);

        var expectedKey = "get:http:localhost:5000:/api/1/users:";
        context.CacheKey.Should().Be(expectedKey);
    }

    [Fact]
    public async Task CreateUpstreamCacheKey_WithDifferentHttpMethods()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var getContext = ETagCacheContextFactory.CreateContext(
            httpMethod: "GET",
            upstreamPath: "/api/1/products");
        await policy.CacheETagAsync(getContext, default);

        var postContext = ETagCacheContextFactory.CreateContext(
            httpMethod: "POST",
            upstreamPath: "/api/1/products");
        await policy.CacheETagAsync(postContext, default);

        getContext.CacheKey.Should().StartWith("get:");
        postContext.CacheKey.Should().StartWith("post:");
        getContext.CacheKey.Should().NotBe(postContext.CacheKey);
    }
}
