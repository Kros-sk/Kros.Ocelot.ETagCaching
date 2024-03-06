using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Text;

namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCachingMiddlewareShould
{
    [Fact]
    public async Task DoNotCacheIfPolicyIsNotSetForDownstreamRoute()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create());

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        store.WasCallSetAsync.Should().BeFalse();
    }

    [Fact]
    public async Task DoNotCacheIfPolicyDoesNotAllowIt()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", builder => builder.AddPolicy(EnableCachePolicy.Disabled)));

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        store.WasCallSetAsync.Should().BeFalse();
    }

    [Fact]
    public async Task CacheDownstreamResponseIfPolicyAllowsIt()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy"));

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        store.WasCallSetAsync.Should().BeTrue();
    }

    [Fact]
    public async Task ServeCacheHeadersIfPolicyAllowsIt()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.ETag(_ => EntityTagHeaderValue.Parse("123"))));

        var context = CreateHttpContext();
        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.Headers.Should().ContainEquivalentOf(new Header("Cache-Control", ["private"]));
        response.Headers.Should().ContainEquivalentOf(new Header("ETag", ["\"123\""]));
    }

    [Fact]
    public async Task ServeNotModifiedResponseIfPolicyAllowsItAndRequestIsNotModified()
    {
        var cacheEntry = new ETagCacheEntry(new ("\"123\""), DateTime.UtcNow);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy"));

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task DoNotServeNotModifiedResponseIfPolicyDoesNotAllowIt()
    {
        var cacheEntry = new ETagCacheEntry(new("\"123\""), DateTime.UtcNow);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.AddPolicy<DisallowNotModifyCachePolicy>()));

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task DoNotServeNotModifiedResponseIfRequestIsModified()
    {
        var cacheEntry = new ETagCacheEntry(new("\"123\""), DateTime.UtcNow);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy"));

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"456\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddCacheFeatureToContextFeatures()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy"));

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        context.Features.Get<ETagCacheFeature>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddCacheHeadersToResponse()
    {
        var cacheEntry = new ETagCacheEntry(new("\"123\""), DateTime.UtcNow);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy"));

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.Headers.Should().ContainEquivalentOf(new Header("Cache-Control", ["private"]));
        response.Headers.Should().ContainEquivalentOf(new Header("ETag", ["\"123\""]));
    }

    private static DownstreamRequest CreateRequest(IEnumerable<(string header, string value)> headers)
    {
        var request = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://localhost"));
        foreach (var (header, value) in headers)
        {
            request.Headers.Add(header, value);
        }

        return request;
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();

        var response = new DownstreamResponse(
            new StringContent(""),
            System.Net.HttpStatusCode.OK,
            new List<Header>(),
            string.Empty);
        context.Items.UpsertDownstreamResponse(response);

        return context;
    }

    private static IOptions<IEnumerable<DownstreamRoute>> CreateRoutes(IEnumerable<DownstreamRoute> routes)
        => Options.Create(routes);

    private class DisallowNotModifyCachePolicy : IETagCachePolicy
    {
        public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            context.AllowNotModified = false;
            return ValueTask.CompletedTask;
        }
    }

    private class Store : IOutputCacheStore
    {
        private readonly ETagCacheEntry? _entry;

        public bool WasCallSetAsync { get; private set; }

        public static Store Create(ETagCacheEntry? entry = null)
            => new(entry);

        private Store(ETagCacheEntry? entry = null)
            => _entry = entry;

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            WasCallSetAsync = true;
            return ValueTask.CompletedTask;
        }

        ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
        {
            byte[]? bytes = null;
            if (_entry is not null)
            {
                bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_entry));
            }

            return ValueTask.FromResult(bytes);
        }
    }
}
