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
        await policy.ServeDownstreamResponseAsync(context, default);

        context.ETag.Should().Be(etag);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new ETagPolicy(_ => new EntityTagHeaderValue("\"123\""));

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
        var extraPropsPolicy = new ETagPolicy(_ => new EntityTagHeaderValue("\"123\""));

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
