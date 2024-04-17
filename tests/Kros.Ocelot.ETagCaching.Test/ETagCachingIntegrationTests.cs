using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCachingIntegrationTests(DefaultWebApplicationFactory factory)
    : IClassFixture<DefaultWebApplicationFactory>
{
    [Fact]
    public async Task EndpointWithCachePolicyShouldReturnETagHeader()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products");

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("ETag").Should().BeTrue();
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenETagIsInvalid()
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/1/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"etag-value\""));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenIfNoneMatchHeaderIsNotPresent()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldNotReturnETagHeader()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("withoutcache/1/products");

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("ETag").Should().BeFalse();
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturnDownstreamData_WhenIfNoneMatchHeaderIsNotPresent()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("this is a body");
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn304NotModified_WhenETagIsValid()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products");

        response.EnsureSuccessStatusCode();
        var etag = response.Headers.ETag!.Tag;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/1/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));

        using var notModifiedResponse = await client.SendAsync(request);

        notModifiedResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenResourceIsCreated()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/2/products");

        response.EnsureSuccessStatusCode();
        var etag = response.Headers.ETag!.Tag;
        var product = new Product
        {
            TenantId = 2,
            Name = "Product",
            Description = "Description",
            Category = "Category",
            Price = 10
        };
        var createResponse = await client.PostAsJsonAsync("/2/products", product);

        createResponse.EnsureSuccessStatusCode();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/2/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));

        var notModifiedResponse = await client.SendAsync(request);

        notModifiedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public class Product
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}
