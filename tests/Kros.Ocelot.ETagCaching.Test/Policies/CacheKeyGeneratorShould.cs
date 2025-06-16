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

    [Fact]
    public void CreateFromDownstreamRequest_ShouldHandleDifferentHttpMethods()
    {
        var methods = new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch };
        var expectedMethods = new[] { "get", "post", "put", "delete", "patch" };

        for (int i = 0; i < methods.Length; i++)
        {
            var request = new HttpRequestMessage()
            {
                Method = methods[i],
                RequestUri = new Uri("https://api.example.com/test")
            };
            var downstreamRequest = new DownstreamRequest(request);

            var result = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

            result.Should().StartWith($"{expectedMethods[i]}:");
        }
    }

    [Fact]
    public void CreateFromUpstreamRequest_ShouldHandleDifferentHttpMethods()
    {
        var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        var expectedMethods = new[] { "get", "post", "put", "delete", "patch" };

        for (int i = 0; i < methods.Length; i++)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = methods[i];
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("api.example.com");
            httpContext.Request.Path = "/test";
            httpContext.Request.QueryString = QueryString.Empty;

            var result = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);

            result.Should().StartWith($"{expectedMethods[i]}:");
        }
    }

    [Fact]
    public void BothMethods_ShouldGenerateDifferentKeysForSameEndpoint()
    {
        // Upstream request
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost:5000");
        httpContext.Request.Path = "/api/products";
        httpContext.Request.QueryString = new QueryString("?page=1");

        // Downstream request (after Ocelot transformation)
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("http://backend:8080/v1/products?page=1")
        };
        var downstreamRequest = new DownstreamRequest(request);

        var upstreamKey = CacheKeyGenerator.CreateFromUpstreamRequest(httpContext.Request);
        var downstreamKey = CacheKeyGenerator.CreateFromDownstreamRequest(downstreamRequest);

        upstreamKey.Should().Be("get:http:localhost:5000:/api/products:?page=1");
        downstreamKey.Should().Be("get:http:backend:/v1/products:?page=1");
        upstreamKey.Should().NotBe(downstreamKey);
    }
}
