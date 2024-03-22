using Kros.Ocelot.ETagCaching.Policies;
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

        context.Should().BeEquivalentTo(context2);
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
                .Excluding(p => p.ETag));
    }
}
