using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class TagTemplatesPolicyShould
{
    [Fact]
    public async Task AddTagsToContext()
    {
        var policy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);
        var context = ETagCacheContextFactory.CreateContext();

        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        AssertHelpers.AssertHashSetIsEquivalent(["tag1:1", "tag2:2"], context.Tags);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);

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
        var extraPropsPolicy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);

        var context = ETagCacheContextFactory.CreateContext();

        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }
}
