using PublicApiGenerator;

namespace Kros.Ocelot.ETagCaching.Test;

public class PublicApiTests
{
    [Fact]
    public Task Assembly_Has_No_Public_Api_Changes()
    {
        var publicApi = typeof(IETagCachingMiddleware).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
