namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCacheContextShould
{
    [Fact]
    public Task OverrideToString()
    {
        var context = ETagCacheContextFactory.CreateContext();

        return Verify(context.ToString());
    }
}
