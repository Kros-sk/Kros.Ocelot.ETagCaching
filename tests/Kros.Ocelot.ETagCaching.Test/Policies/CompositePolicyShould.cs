using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CompositePolicyShould
{
    [Fact]
    public async Task CombineMultiplePoliciesIntoOne()
    {
        var policy1 = new HelperPolicy(
            (c) =>
            {
                c.CacheEntryExtraProps.Add("CacheETag", "policy1");
                c.CacheEntryExtraProps.Add("policy1-CacheETag", 1);
            },
            (c) =>
            {
                c.CacheEntryExtraProps.Add("ServeNotModified", "policy1");
                c.CacheEntryExtraProps.Add("policy1-ServeNotModified", 1);
            },
            (c) =>
            {
                c.CacheEntryExtraProps.Add("ServeDownstreamResponse", "policy1");
                c.CacheEntryExtraProps.Add("policy1-ServeDownstreamResponse", 1);
            });
        var policy2 = new HelperPolicy(
            (c) =>
            {
                c.CacheEntryExtraProps["CacheETag"] = "policy2";
                c.CacheEntryExtraProps["policy2-CacheETag"] = 2;
            },
            (c) =>
            {
                c.CacheEntryExtraProps["ServeNotModified"] = "policy2";
                c.CacheEntryExtraProps["policy2-ServeNotModified"] = 2;
            },
            (c) =>
            {
                c.CacheEntryExtraProps["ServeDownstreamResponse"] = "policy2";
                c.CacheEntryExtraProps["policy2-ServeDownstreamResponse"] = 2;
            });
        var policy3 = new HelperPolicy(
            (c) =>
            {
                c.CacheEntryExtraProps["CacheETag"] = "policy3";
                c.CacheEntryExtraProps["policy3-CacheETag"] = 3;
            },
            (c) =>
            {
                c.CacheEntryExtraProps["ServeNotModified"] = "policy3";
                c.CacheEntryExtraProps["policy3-ServeNotModified"] = 3;
            },
            (c) =>
            {
                c.CacheEntryExtraProps["ServeDownstreamResponse"] = "policy3";
                c.CacheEntryExtraProps["policy3-ServeDownstreamResponse"] = 3;
            });

        var compositePolicy = new CompositePolicy([policy1, policy2, policy3]);

        var context = ETagCacheContextFactory.CreateContext();
        await compositePolicy.CacheETagAsync(context, default);
        await compositePolicy.ServeDownstreamResponseAsync(context, default);
        await compositePolicy.ServeNotModifiedAsync(context, default);

        context.CacheEntryExtraProps.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            { "CacheETag", "policy3" },
            { "policy1-CacheETag", 1 },
            { "policy2-CacheETag", 2 },
            { "policy3-CacheETag", 3 },
            { "ServeNotModified", "policy3" },
            { "policy1-ServeNotModified", 1 },
            { "policy2-ServeNotModified", 2 },
            { "policy3-ServeNotModified", 3 },
            { "ServeDownstreamResponse", "policy3" },
            { "policy1-ServeDownstreamResponse", 1 },
            { "policy2-ServeDownstreamResponse", 2 },
            { "policy3-ServeDownstreamResponse", 3 }
        });
    }

    private class HelperPolicy(
        Action<ETagCacheContext> cacheEtag,
        Action<ETagCacheContext> serveNoMofied,
        Action<ETagCacheContext> serveDownstreamResponse) : IETagCachePolicy
    {
        public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            cacheEtag(context);
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            serveNoMofied(context);
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            serveDownstreamResponse(context);
            return ValueTask.CompletedTask;
        }
    }
}
