using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class ExpirationPolicyShould
{
    [Fact]
    public async Task SetExpirationTimeToContext()
    {
        var policy = new ExpirationPolicy(TimeSpan.FromMinutes(5));
        var context = ETagCacheContextFactory.CreateContext();

        await policy.CacheETagAsync(context, default);

        context.ETagExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = new DefaultPolicy();
        var extraPropsPolicy = new ExpirationPolicy(TimeSpan.FromMinutes(5));

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
        var extraPropsPolicy = new ExpirationPolicy(TimeSpan.FromMinutes(5));

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
