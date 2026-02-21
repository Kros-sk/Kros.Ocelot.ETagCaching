using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class InvalidateDefaultPolicyShould
{
    [Theory]
    [InlineData("GET", false)]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    [InlineData("DELETE", true)]
    [InlineData("PATCH", true)]
    [InlineData("HEAD", false)]
    [InlineData("OPTIONS", false)]
    public async Task SetInvalidatedWhenIsModifyMethod(string method, bool invalidated)
    {
        var context = InvalidateCacheContextFactory.CreateContext(method);
        var policy = new InvalidateDefaultPolicy(["products"]);

        await policy.InvalidateCacheAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(invalidated, context.AllowCacheInvalidation);
    }

    [Fact]
    public async Task AddTagsByTemplate()
    {
        var context = InvalidateCacheContextFactory.CreateContext();
        var policy = new InvalidateDefaultPolicy(["products:{tenantId}", "products:{tenantId}:{id}"]);

        await policy.InvalidateCacheAsync(context, TestContext.Current.CancellationToken);

        AssertHelpers.AssertHashSetIsEquivalent(["products:1", "products:1:2"], context.Tags);
    }
}
