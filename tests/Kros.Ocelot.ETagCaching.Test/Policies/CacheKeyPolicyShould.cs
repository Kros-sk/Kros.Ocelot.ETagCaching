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
        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(keyGenerator(context.DownstreamRequest), context.CacheKey);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheKeyPolicy((_) => string.Empty);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeNotModifiedAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheKeyPolicy((_) => string.Empty);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }

    [Fact]
    public async Task CreateUpstreamCacheKeyInContext_WhenFromUpstreamRequestWithCustomGeneratorWasUsed()
    {
        var keyGenerator = new Func<HttpRequest, string>(r => $"custom:{r.Method}:{r.Path}");
        var policy = CacheKeyPolicy.FromUpstreamRequest(keyGenerator);

        var context = ETagCacheContextFactory.CreateContext(
            upstreamPath: "/api/1/products",
            upstreamQuery: "?filter=active");
        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal("custom:GET:/api/1/products", context.CacheKey);
    }

    [Fact]
    public async Task CreateDefaultUpstreamCacheKeyInContext_WhenFromUpstreamRequestWithoutGeneratorWasUsed()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var context = ETagCacheContextFactory.CreateContext(
            httpMethod: "POST",
            upstreamPath: "/api/1/orders",
            upstreamQuery: "?include=details");
        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        var expectedKey = "post:http:localhost:5000:/api/1/orders:?include=details";
        Assert.Equal(expectedKey, context.CacheKey);
    }

    [Fact]
    public async Task CreateUpstreamCacheKey_WithEmptyQueryString()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var context = ETagCacheContextFactory.CreateContext(
            upstreamPath: "/api/1/users",
            upstreamQuery: "");
        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        var expectedKey = "get:http:localhost:5000:/api/1/users:";
        Assert.Equal(expectedKey, context.CacheKey);
    }

    [Fact]
    public async Task CreateUpstreamCacheKey_WithDifferentHttpMethods()
    {
        var policy = CacheKeyPolicy.FromUpstreamRequest();

        var getContext = ETagCacheContextFactory.CreateContext(
            httpMethod: "GET",
            upstreamPath: "/api/1/products");
        await policy.CacheETagAsync(getContext, TestContext.Current.CancellationToken);

        var postContext = ETagCacheContextFactory.CreateContext(
            httpMethod: "POST",
            upstreamPath: "/api/1/products");
        await policy.CacheETagAsync(postContext, TestContext.Current.CancellationToken);

        Assert.StartsWith("get:", getContext.CacheKey);
        Assert.StartsWith("post:", postContext.CacheKey);
        Assert.NotEqual(postContext.CacheKey, getContext.CacheKey);
    }
}
