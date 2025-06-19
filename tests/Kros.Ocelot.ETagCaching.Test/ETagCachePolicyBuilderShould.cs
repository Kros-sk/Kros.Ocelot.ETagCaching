using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCachePolicyBuilderShould
{
    [Fact]
    public async Task BuildPolicy()
    {
        var builder = new ETagCachePolicyBuilder(true);

        builder.Expire(TimeSpan.FromMinutes(10))
            .CacheControl(new CacheControlHeaderValue
            {
                MaxAge = TimeSpan.FromMinutes(10),
                Private = true
            })
            .ETag(_ => new EntityTagHeaderValue("\"123\""))
            .CacheKey(_ => "cacheKey")
            .TagTemplates("tag1:{tenantId}", "tag2:{id}")
            .StatusCode(222)
            .CacheEntryExtraProp(props => props.Add("key1", "value1"));

        var policy = builder.Build();
        var context = ETagCacheContextFactory.CreateContext();

        await policy.CacheETagAsync(context, default);
        await policy.ServeDownstreamResponseAsync(context, default);
        await policy.ServeNotModifiedAsync(context, default);

        context.ETag.Should().Be(new EntityTagHeaderValue("\"123\""));
        context.ETagExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(10));
        context.ResponseHeaders.Should().Contain(HeaderNames.CacheControl, "max-age=600, private");
        context.CacheKey.Should().Be("cacheKey");
        context.Tags.Should().BeEquivalentTo(["tag1:1", "tag2:2"]);
        context.StatusCode.Should().Be((HttpStatusCode)222);
        context.CacheEntryExtraProps.Should().Contain("key1", "value1");
    }

    [Fact]
    public void BuildDefaultPolicy()
    {
        var builder = new ETagCachePolicyBuilder();

        var policy = builder.Build();

        policy.Should().Be(DefaultPolicy.Instance);
    }

    [Fact]
    public void BuildEmptyPolicyIfNotDefaultPolicyIsAlowed()
    {
        var builder = new ETagCachePolicyBuilder(true);

        var policy = builder.Build();

        policy.Should().Be(EmptyPolicy.Instance);
    }

    [Fact]
    public async Task BuildCustomPolicy()
    {
        var builder = new ETagCachePolicyBuilder(true);

        builder.AddPolicy<CustomPolicy>();

        var policy = builder.Build();
        var context = ETagCacheContextFactory.CreateContext();

        await policy.CacheETagAsync(context, default);

        context.CacheKey.Should().Be("custom");
    }

    private class CustomPolicy : IETagCachePolicy
    {
        public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            context.CacheKey = "custom";
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task BuildUpstreamCacheKeyPolicy_WithCustomGenerator()
    {
        var builder = new ETagCachePolicyBuilder(true);

        builder.UpstreamCacheKey(request => $"upstream:{request.Method}:{request.Path}");

        var policy = builder.Build();
        var context = ETagCacheContextFactory.CreateContext(
            httpMethod: "POST",
            upstreamPath: "/api/1/orders");

        await policy.CacheETagAsync(context, default);

        context.CacheKey.Should().Be("upstream:POST:/api/1/orders");
    }

    [Fact]
    public async Task BuildUpstreamCacheKeyPolicy_WithDefaultGenerator()
    {
        var builder = new ETagCachePolicyBuilder(true);

        builder.UpstreamCacheKey();

        var policy = builder.Build();
        var context = ETagCacheContextFactory.CreateContext(
            httpMethod: "GET",
            upstreamPath: "/api/1/products",
            upstreamQuery: "?category=electronics");

        await policy.CacheETagAsync(context, default);

        var expectedKey = "get:http:localhost:5000:/api/1/products:?category=electronics";
        context.CacheKey.Should().Be(expectedKey);
    }

    [Fact]
    public async Task BuildUpstreamCacheKeyPolicy_ShouldChainWithOtherPolicies()
    {
        var builder = new ETagCachePolicyBuilder(true);

        builder.UpstreamCacheKey(request => $"chain:{request.Path}")
            .Expire(TimeSpan.FromMinutes(5))
            .StatusCode(304);

        var policy = builder.Build();
        var context = ETagCacheContextFactory.CreateContext(
            upstreamPath: "/api/1/users");

        await policy.CacheETagAsync(context, default);
        await policy.ServeNotModifiedAsync(context, default);

        context.CacheKey.Should().Be("chain:/api/1/users");
        context.ETagExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        context.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }
}
