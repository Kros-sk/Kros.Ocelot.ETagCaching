using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CacheControlPolicyShould
{
    [Fact]
    public async Task AddCacheControlHeaderToResponse_WhenServeDownstreamResponseAsyncWasCall()
    {
        var cacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromSeconds(10),
            Private = true
        };
        var policy = new CacheControlPolicy(cacheControl);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.ServeDownstreamResponseAsync(context, default);

        context.ResponseHeaders.Should().Contain(HeaderNames.CacheControl, cacheControl.ToString());
    }

    [Fact]
    public async Task AddCacheControlHeaderToResponse_WhenServeNotModifiedAsyncWasCall()
    {
        var cacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromSeconds(10),
            Private = true
        };
        var policy = new CacheControlPolicy(cacheControl);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.ServeNotModifiedAsync(context, default);

        context.ResponseHeaders.Should().Contain(HeaderNames.CacheControl, cacheControl.ToString());
    }

    [Fact]
    public async Task NotChangeContextState_WhenCacheETagAsyncWasCall()
    {
        var cacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromSeconds(10),
            Private = true
        };
        var policy = new CacheControlPolicy(cacheControl);
        var defaultPolicy = DefaultPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context2, default);
        await policy.CacheETagAsync(context2, default);

        context.Should().BeEquivalentTo(context2);
    }
}
