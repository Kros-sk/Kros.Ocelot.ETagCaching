using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class InvalidateEmptyCachePolicyShould
{
    [Fact]
    public async Task DoNotInvalidateCache()
    {
        var context = InvalidateCacheContextFactory.CreateContext();
        var policy = new InvalidateEmptyPolicy();

        await policy.InvalidateCacheAsync(context, default);

        context.AllowCacheInvalidation.Should().BeFalse();
    }
}
