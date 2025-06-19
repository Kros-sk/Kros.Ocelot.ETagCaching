using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.AspNetCore.Http;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class CacheKeyGeneratorShould
{
    [Fact]
    public void CreateFromDownstreamRequest_ShouldGenerateCorrectKey()
    {
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://api.example.com/products?category=electronics&page=1")
        };
        var downstreamRequest = new DownstreamRequest(request);

        var result = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

        result.Should().Be("get:https:api.example.com:/products:?category=electronics&page=1");
    }

    [Fact]
    public void CreateFromUpstreamRequest_ShouldGenerateCorrectKey()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost:5000");
        httpContext.Request.Path = "/api/v1/orders";
        httpContext.Request.QueryString = new QueryString("?include=details&format=json");

        var result = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);

        result.Should().Be("post:http:localhost:5000:/api/v1/orders:?include=details&format=json");
    }

    [Fact]
    public void CreateFromDownstreamRequest_ShouldHandleEmptyQuery()
    {
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri("https://api.example.com/users/123")
        };
        var downstreamRequest = new DownstreamRequest(request);

        var result = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

        result.Should().Be("delete:https:api.example.com:/users/123:");
    }

    [Fact]
    public void CreateFromUpstreamRequest_ShouldHandleEmptyQuery()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "PUT";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.myapp.com");
        httpContext.Request.Path = "/api/users/456";
        httpContext.Request.QueryString = QueryString.Empty;

        var result = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);

        result.Should().Be("put:https:api.myapp.com:/api/users/456:");
    }

    [Fact]
    public void CreateFromDownstreamRequest_ShouldNormalizeToLowerCase()
    {
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("HTTPS://API.EXAMPLE.COM/PRODUCTS?CATEGORY=ELECTRONICS")
        };
        var downstreamRequest = new DownstreamRequest(request);

        var result = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

        result.Should().Be("get:https:api.example.com:/products:?category=electronics");
    }

    [Fact]
    public void CreateFromUpstreamRequest_ShouldNormalizeToLowerCase()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "HTTPS";
        httpContext.Request.Host = new HostString("API.MYAPP.COM");
        httpContext.Request.Path = "/API/PRODUCTS";
        httpContext.Request.QueryString = new QueryString("?CATEGORY=ELECTRONICS");

        var result = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);

        result.Should().Be("get:https:api.myapp.com:/api/products:?category=electronics");
    }

    [Theory]
    [InlineData("GET", "get")]
    [InlineData("POST", "post")]
    [InlineData("PUT", "put")]
    [InlineData("DELETE", "delete")]
    [InlineData("PATCH", "patch")]
    public void CreateFromDownstreamRequest_ShouldHandleDifferentHttpMethods(string methodName, string expectedMethod)
    {
        var method = new HttpMethod(methodName);
        var request = new HttpRequestMessage()
        {
            Method = method,
            RequestUri = new Uri("https://api.example.com/test")
        };
        var downstreamRequest = new DownstreamRequest(request);

        var result = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

        result.Should().StartWith($"{expectedMethod}:");
    }

    [Theory]
    [InlineData("GET", "get")]
    [InlineData("POST", "post")]
    [InlineData("PUT", "put")]
    [InlineData("DELETE", "delete")]
    [InlineData("PATCH", "patch")]
    public void CreateFromUpstreamRequest_ShouldHandleDifferentHttpMethods(string method, string expectedMethod)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.com");
        httpContext.Request.Path = "/test";
        httpContext.Request.QueryString = QueryString.Empty;

        var result = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);

        result.Should().StartWith($"{expectedMethod}:");
    }
}
