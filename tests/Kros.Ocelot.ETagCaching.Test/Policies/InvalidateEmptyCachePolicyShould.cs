using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class InvalidateEmptyCachePolicyShould
{
    [Fact]
    public async Task DoNotInvalidateCache()
    {
        var context = InvalidateCacheContextFactory.CreateContext();
        var policy = new InvalidateEmptyPolicy();

        await policy.InvalidateCacheAsync(context, TestContext.Current.CancellationToken);

        Assert.False(context.AllowCacheInvalidation);
    }
}
