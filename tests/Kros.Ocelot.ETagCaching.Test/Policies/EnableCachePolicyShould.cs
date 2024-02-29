using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class EnableCachePolicyShould
{
    [Fact]
    public async Task EnabledEnableCachePolicy()
    {
        var policy = EnableCachePolicy.Enabled;
        var context = ETagCacheContextFactory.CreateContext();
        context.EnableETagCache = false;

        await policy.CacheETagAsync(context, default);

        context.EnableETagCache.Should().BeTrue();
    }

    [Fact]
    public async Task DisabledDisableCachePolicy()
    {
        var policy = EnableCachePolicy.Disabled;
        var context = ETagCacheContextFactory.CreateContext();
        context.EnableETagCache = true;

        await policy.CacheETagAsync(context, default);

        context.EnableETagCache.Should().BeFalse();
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = new DefaultPolicy();
        var extraPropsPolicy = EnableCachePolicy.Enabled;

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
        var defaultPolicy = new DefaultPolicy();
        var extraPropsPolicy = EnableCachePolicy.Enabled;

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
