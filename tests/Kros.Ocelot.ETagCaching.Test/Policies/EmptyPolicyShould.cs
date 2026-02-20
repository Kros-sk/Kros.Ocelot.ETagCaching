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
        await defaultPolicy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.CacheETagAsync(context2, TestContext.Current.CancellationToken);
        await emptyPolicy.CacheETagAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var emptyPolicy = EmptyPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context2, TestContext.Current.CancellationToken);
        await emptyPolicy.ServeNotModifiedAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var emptyPolicy = EmptyPolicy.Instance;

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await emptyPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }
}
