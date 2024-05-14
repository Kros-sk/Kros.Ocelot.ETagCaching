using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching.Test;

internal static class InvalidateCacheContextFactory
{
    public static InvalidateCacheContext CreateContext(string httpMethod = "POST")
    {
        var httpContext = new DefaultHttpContext();
        var request = new HttpRequestMessage()
        {
            Method = new HttpMethod(httpMethod),
            RequestUri = new Uri("http://localhost:5000/api/2/products?skip=10&take=5")
        };
        var context = new InvalidateCacheContext()
        {
            RequestFeatures = httpContext.Features,
            RequestServices = httpContext.RequestServices,
            TemplatePlaceholderNameAndValues = [],
            DownstreamRequest = new DownstreamRequest(request)
        };

        context.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{tenantId}", "1"));
        context.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{id}", "2"));

        return context;
    }
}
