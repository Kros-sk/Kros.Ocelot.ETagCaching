using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class EmptyPolicyShould
{
    [Fact]
    public async Task NotChangeContextState_WhenCacheETagAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var emptyPolicy = EmptyPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context2, default);
        await emptyPolicy.CacheETagAsync(context2, default);

        context.Should().BeEquivalentTo(context2, o =>
            o.Excluding(p => p.HttpContext));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var emptyPolicy = EmptyPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context2, default);
        await emptyPolicy.ServeNotModifiedAsync(context2, default);

        context.Should().BeEquivalentTo(context2, o =>
            o.Excluding(p => p.HttpContext));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var emptyPolicy = EmptyPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, default);
        await emptyPolicy.ServeDownstreamResponseAsync(context2, default);

        context.Should().BeEquivalentTo(context2, options
            => options
                .Excluding(p => p.ResponseHeaders)
                .Excluding(p=> p.ETag)
                .Excluding(p => p.HttpContext));
    }
}
