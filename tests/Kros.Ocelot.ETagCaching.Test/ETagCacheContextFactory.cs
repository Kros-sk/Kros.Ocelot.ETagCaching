using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Net;

namespace Kros.Ocelot.ETagCaching.Test;

internal static class ETagCacheContextFactory
{
    public static ETagCacheContext CreateContext(
        string httpMethod = "GET",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string etagValue = "default",
        string? upstreamPath = null,
        string? upstreamQuery = null)
    {
        var httpContext = new DefaultHttpContext { Request =
            {
                Method = httpMethod,
                Scheme = "http",
                Host = new HostString("localhost:5000"),
                Path = upstreamPath ?? "/api/1/products",
                QueryString = new QueryString(upstreamQuery ?? "?skip=10&take=5")
            }
        };

        var request = new HttpRequestMessage()
        {
            Method = new HttpMethod(httpMethod),
            RequestUri = new Uri("http://localhost:5000/api/2/products?skip=10&take=5")
        };
        var context = new ETagCacheContext()
        {
            RequestFeatures = httpContext.Features,
            RequestServices = httpContext.RequestServices,
            HttpContext = httpContext,
            TemplatePlaceholderNameAndValues = [],
            DownstreamRequest = new DownstreamRequest(request),
            DownstreamResponse = new DownstreamResponse(new HttpResponseMessage(statusCode)),
            ETag = new EntityTagHeaderValue($"\"{etagValue}\"")
        };

        context.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{tenantId}", "1"));
        context.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{id}", "2"));

        return context;
    }
}
