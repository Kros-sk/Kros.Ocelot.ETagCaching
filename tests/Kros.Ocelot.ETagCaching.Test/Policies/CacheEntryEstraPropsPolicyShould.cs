using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CacheEntryEstraPropsPolicyShould
{
    [Fact]
    public async Task AddExtraPropsToCacheEntry()
    {
        var extraProps = new Dictionary<string, object>
        {
            { "prop1", "value1" },
            { "prop2", 2 }
        };
        var policy = new CacheEntryExtraPropsPolicy(extraProps);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.CacheETagAsync(context, default);

        context.CacheEntryExtraProps.Should().BeEquivalentTo(extraProps);
    }

    [Fact]
    public async Task AddMultipleExtraPropsToCacheEntry()
    {
        var extraProps = new Dictionary<string, object>
        {
            { "prop1", "value1" },
            { "prop2", 2 }
        };
        var extraProps2 = new Dictionary<string, object>
        {
            { "prop2", 33 },
            { "prop3", "value3" },
            { "prop4", 4 }
        };

        var context = ETagCacheContextFactory.CreateContext();

        await new CacheEntryExtraPropsPolicy(extraProps).CacheETagAsync(context, default);
        await new CacheEntryExtraPropsPolicy(extraProps2).CacheETagAsync(context, default);

        var expected = new Dictionary<string, object>
        {
            { "prop1", "value1" },
            { "prop2", 33 },
            { "prop3", "value3" },
            { "prop4", 4 }
        };
        context.CacheEntryExtraProps.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheEntryExtraPropsPolicy(new Dictionary<string, object>());

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
        var extraPropsPolicy = new CacheEntryExtraPropsPolicy(new Dictionary<string, object>());

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
