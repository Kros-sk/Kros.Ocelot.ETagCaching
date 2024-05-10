using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class InvalidateCompositPolicyShould
{
    [Fact]
    public async Task InvalidateCache()
    {
        var context = InvalidateCacheContextFactory.CreateContext();
        var policy1 = new InvalidateEmptyPolicy();
        var policy2 = new InvalidateDefaultPolicy(["products"]);
        var policy = new InvalidateCompositePolicy([policy1, policy2]);

        await policy.InvalidateCacheAsync(context, default);

        context.AllowCacheInvalidation.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateCacheByAllPolicies()
    {
        var context = InvalidateCacheContextFactory.CreateContext();
        var policy1 = new InvalidateDefaultPolicy(["products"]);
        var policy2 = new InvalidateDefaultPolicy(["products:{tenantId}", "products:{tenantId}:{id}"]);
        var policy = new InvalidateCompositePolicy([policy1, policy2]);

        await policy.InvalidateCacheAsync(context, default);

        context.AllowCacheInvalidation.Should().BeTrue();
        context.Tags.Should().BeEquivalentTo(["products", "products:1", "products:1:2"]);
    }
}
