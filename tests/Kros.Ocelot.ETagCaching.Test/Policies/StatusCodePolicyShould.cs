using Kros.Ocelot.ETagCaching.Policies;
using System.Net;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class StatusCodePolicyShould
{
    [Fact]
    public async Task ReturnStatusCode_WhenServeNotModifiedAsyncWasCall()
    {
        var policy = new StatusCodePolicy(211);
        var context = ETagCacheContextFactory.CreateContext();

        await policy.ServeNotModifiedAsync(context, default);

        context.StatusCode.Should().Be((HttpStatusCode)211);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new StatusCodePolicy(211);

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

    [Fact]
    public async Task NotChangeContextState_WhenCacheETagAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new StatusCodePolicy(211);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context2, default);
        await extraPropsPolicy.CacheETagAsync(context2, default);

        context.Should().BeEquivalentTo(context2);
    }
}
