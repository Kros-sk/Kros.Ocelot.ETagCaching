using EShop.Gateway;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Kros.Ocelot.ETagCaching.Test;

public class DefaultWebApplicationFactory : WebApplicationFactory<IAssemblyMarker>
{
    public WireMockServer WireMockServer { get; }

    public DefaultWebApplicationFactory()
    {
        WireMockServer = WireMockServer.Start();
        ConfigureWireMockServer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var configDict = GetConfiguration();

            config.AddInMemoryCollection(configDict);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WireMockServer.Stop();
            WireMockServer.Dispose();
        }

        base.Dispose(disposing);
    }

    private Dictionary<string, string?> GetConfiguration()
    {
        var ret = new Dictionary<string, string?>();

        for (var i = 0; i < 3; i++)
        {
            ret[$"Routes:{i}:DownstreamHostAndPorts:0:Host"] = "localhost";
            ret[$"Routes:{i}:DownstreamHostAndPorts:0:Port"] = WireMockServer.Ports[0].ToString();
        }

        return ret;
    }

    private void ConfigureWireMockServer()
    {
        WireMockServer.Given(Request.Create().WithPath("/1/products").UsingGet())
            .RespondWith(Response.Create().WithBody("this is a body").WithStatusCode(200));
    }
}
