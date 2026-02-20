using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CacheEntryEstraPropsPolicyShould
{
    [Fact]
    public async Task AddExtraPropsToCacheEntry()
    {
        var extraProps = new Dictionary<string, object?>
        {
            { "prop1", "value1" },
            { "prop2", 2 }
        };
        var policy = new CacheEntryExtraPropsPolicy(extraProps);

        var context = ETagCacheContextFactory.CreateContext();
        await policy.CacheETagAsync(context, TestContext.Current.CancellationToken);

        AssertHelpers.AssertDictionaryEquivalent(extraProps, context.CacheEntryExtraProps);
    }

    [Fact]
    public async Task AddMultipleExtraPropsToCacheEntry()
    {
        var extraProps = new Dictionary<string, object?>
        {
            { "prop1", "value1" },
            { "prop2", 2 }
        };
        var extraProps2 = new Dictionary<string, object?>
        {
            { "prop2", 33 },
            { "prop3", "value3" },
            { "prop4", 4 }
        };

        var context = ETagCacheContextFactory.CreateContext();

        await new CacheEntryExtraPropsPolicy(extraProps).CacheETagAsync(context, TestContext.Current.CancellationToken);
        await new CacheEntryExtraPropsPolicy(extraProps2).CacheETagAsync(context, TestContext.Current.CancellationToken);

        var expected = new Dictionary<string, object?>
        {
            { "prop1", "value1" },
            { "prop2", 33 },
            { "prop3", "value3" },
            { "prop4", 4 }
        };
        AssertHelpers.AssertDictionaryEquivalent(expected, context.CacheEntryExtraProps);
    }

    [Fact]
    public async Task NotChangeContextState_WhenServeNotModifiedAsyncWasCall()
    {
        var defaultPolicy = DefaultPolicy.Instance;
        var extraPropsPolicy = new CacheEntryExtraPropsPolicy(new Dictionary<string, object>());

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
        var extraPropsPolicy = new CacheEntryExtraPropsPolicy(new Dictionary<string, object>());

        var context = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context, TestContext.Current.CancellationToken);

        var context2 = ETagCacheContextFactory.CreateContext();
        await defaultPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);
        await extraPropsPolicy.ServeDownstreamResponseAsync(context2, TestContext.Current.CancellationToken);

        AssertHelpers.AssertContextEqual(context, context2, excludeResponseHeaders: true, excludeETag: true);
    }
}
