using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class TagTemplatesPolicyShould
{
    [Fact]
    public async Task AddTagsToContext()
    {
        var policy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);
        var context = ETagCacheContextFactory.CreateContext();

        await policy.CacheETagAsync(context, default);

        context.Tags.Should().BeEquivalentTo(["tag1:1", "tag2:2"]);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);

        var context = ETagCacheContextFactory.CreateContext();

        await defaultPolicy.ServeNotModifiedAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeNotModifiedAsync(context2, default);
        await extraPropsPolicy.ServeNotModifiedAsync(context2, default);

        context.Should().BeEquivalentTo(context2,
            o => o.Excluding(p => p.HttpContext));
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeDownstreamResponseAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new TagTemplatesPolicy(["tag1:{tenantId}", "tag2:{id}"]);

        var context = ETagCacheContextFactory.CreateContext();

        await defaultPolicy.ServeDownstreamResponseAsync(context, default);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, default);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, default);

        context.Should().BeEquivalentTo(context2,
            options => options
                .Excluding(p => p.ResponseHeaders)
                .Excluding(p => p.ETag)
                .Excluding(p => p.HttpContext));
    }
}
