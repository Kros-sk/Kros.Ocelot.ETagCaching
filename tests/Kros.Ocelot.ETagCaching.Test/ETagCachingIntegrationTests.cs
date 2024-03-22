using System.Net;
using System.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCachingIntegrationTests(DefaultWebApplicationFactory factory)
    : IClassFixture<DefaultWebApplicationFactory>
{
    [Fact]
    public async Task EndpointWithCachePolicyShouldReturnETagHeader()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/1/products");

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("ETag").Should().BeTrue();
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OKWhenETagIsInvalid()
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/1/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"etag-value\""));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OKWhenIfNoneMatchHeaderIsNotPresent()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldNotReturnETagHeader()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("withoutcache/1/products");

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("ETag").Should().BeFalse();
    }

    // Endpoint with cache policy should return downstream data when ETag is invalid

    // Endpoint with cache policy should return 304 Not Modified when ETag is valid
}
