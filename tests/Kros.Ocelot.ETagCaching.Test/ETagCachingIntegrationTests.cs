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

        using var response = await client.GetAsync("/1/products", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.Contains("ETag", response.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenETagIsInvalid()
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/1/products/");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"etag-value\""));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenIfNoneMatchHeaderIsNotPresent()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldNotReturnETagHeader()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("withoutcache/1/products/", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturnDownstreamData_WhenIfNoneMatchHeaderIsNotPresent()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("this is a body", content);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn304NotModified_WhenETagIsValid()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/1/products", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var etag = response.Headers.ETag!.Tag;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/1/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));

        using var notModifiedResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, notModifiedResponse.StatusCode);
    }

    [Fact]
    public async Task EndpointWithCachePolicyShouldReturn200OK_WhenResourceIsCreated()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/2/products/", TestContext.Current.CancellationToken);

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
        var createResponse = await client.PostAsJsonAsync("/2/products/", product, TestContext.Current.CancellationToken);

        createResponse.EnsureSuccessStatusCode();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/2/products");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));

        var notModifiedResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, notModifiedResponse.StatusCode);
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
