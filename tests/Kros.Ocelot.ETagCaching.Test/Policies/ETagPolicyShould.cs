using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class ETagPolicyShould
{
    [Fact]
    public async Task GenerateETag()
    {
        var etag = new EntityTagHeaderValue("\"123\"");
        var policy = new ETagPolicy(_ => etag);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(etag, context.ETag);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new ETagPolicy(_ => new EntityTagHeaderValue("\"123\""));

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
        var extraPropsPolicy = new ETagPolicy(_ => new EntityTagHeaderValue("\"123\""));

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }
}
