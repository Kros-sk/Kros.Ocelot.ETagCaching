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

        await policy.ServeNotModifiedAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal((HttpStatusCode)211, context.StatusCode);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new StatusCodePolicy(211);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }

    [Fact]
    public async Task NotChangeContextState_WhenCacheETagAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new StatusCodePolicy(211);

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.CacheETagAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2);
    }
}
