using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Middleware;

namespace Kros.Ocelot.ETagCaching.Test;

public class OcelotPipelineConfigurationExtensionShould
{
    [Fact]
    public async Task SetsPreQueryStringBuilderMiddleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration();
        var serviceCollection = new ServiceCollection();
        var eTagCachingMiddleware = Substitute.For<IETagCachingMiddleware>();
        serviceCollection.AddSingleton(eTagCachingMiddleware);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        pipelineConfiguration.AddETagCaching();

        await pipelineConfiguration.PreQueryStringBuilderMiddleware(httpContext, () => Task.CompletedTask);
        await eTagCachingMiddleware.Received(1).InvokeAsync(httpContext, Arg.Any<Func<Task>>());
    }
}
