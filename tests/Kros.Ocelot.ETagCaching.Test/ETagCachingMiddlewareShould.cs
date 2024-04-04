using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;

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
            Options.Create(ETagCachingOptions.Create()),
            NullLogger<ETagCachingMiddleware>.Instance);
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
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", builder => builder.AddPolicy(EnableCachePolicy.Disabled))),
            NullLogger<ETagCachingMiddleware>.Instance);

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
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

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
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.ETag(_ => new("\"123\"")))),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.Headers.Should().ContainEquivalentOf(new Header("Cache-Control", ["private"]));
        response.Headers.Should().ContainEquivalentOf(new Header("ETag", ["\"123\""]));
    }

    [Fact]
    public async Task ServeNotModifiedResponseIfPolicyAllowsItAndRequestIsNotModified()
    {
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task DoNotServeNotModifiedResponseIfPolicyDoesNotAllowIt()
    {
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.AddPolicy<DisallowNotModifyCachePolicy>())),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task DoNotServeNotModifiedResponseIfRequestIsModified()
    {
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

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
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);
        var feature = context.Features.Get<ETagCacheFeature>()!;
        feature.Should().NotBeNull();
        feature.ETagCacheContext.Should().NotBeNull();
    }

    [Fact]
    public async Task AddCacheHeadersToResponse()
    {
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.ETag(_ => new("\"123\"")))),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.Headers.Should().ContainEquivalentOf(new Header("Cache-Control", ["private"]));
        response.Headers.Should().ContainEquivalentOf(new Header("ETag", ["\"123\""]));
    }

    [Fact]
    public async Task CallNextMiddlewareWhenPolicyIsNotSetForDownstreamRoute()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        var nextCalled = false;

        await middleware.InvokeAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CallNextMiddlewareWhenPolicyDoesNotAllowCaching()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", builder => builder.AddPolicy(EnableCachePolicy.Disabled))),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        var nextCalled = false;

        await middleware.InvokeAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CallNextMiddlewareWhenPolicyAllowsCachingAndNeedDownstreamResponse()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        var nextCalled = false;

        await middleware.InvokeAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DoNotCallNextMiddlewareWhenNotModifiedResponseIsServed()
    {
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));
        var nextCalled = false;

        await middleware.InvokeAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task CacheDownstreamResponseBeforeServingNotModifiedResponse()
    {
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.ETag(_ => new("\"123\"")))),
            NullLogger<ETagCachingMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        var response = context.Items.DownstreamResponse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task LoginformationsWhenServeFromCache()
    {
        var logger = new FakeLogger();
        var store = Store.Create();
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddPolicy("productsPolicy", b => b.ETag(_ => new("\"123\"")))),
            logger);

        var context = CreateHttpContext();
        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        logger.ExecuteCount.Should().Be(5);
    }

    [Fact]
    public async Task LoginformationsWhenServeNotModified()
    {
        var logger = new FakeLogger();
        var cacheEntry = new ETagCacheEntry(new EntityTagHeaderValue("\"123\""), []);
        var store = Store.Create(cacheEntry);
        var middleware = new ETagCachingMiddleware(
            CreateRoutes([new("products", "productsPolicy")]),
            store,
            Options.Create(ETagCachingOptions.Create()
                .AddDefaultPolicy("productsPolicy")),
            logger);

        var context = CreateHttpContext();
        context.Items.UpsertDownstreamRequest(CreateRequest([("If-None-Match", "\"123\"")]));

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        logger.ExecuteCount.Should().Be(4);
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

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();

        var response = new DownstreamResponse(
            new StringContent(""),
            System.Net.HttpStatusCode.OK,
            new List<Header>(),
            string.Empty);
        context.Items.UpsertDownstreamResponse(response);
        context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://localhost")));
        context.Items.UpsertDownstreamRoute(new DownstreamRoute("products", null, null, null, [],
            null, null, null, false, false, null, null, null,
            false, null, null, null, [], [], [], [], [], false,
            false, null, null, null, [], [], [], false, null, null, null));

        return context;
    }

    private static IOptions<List<FakeDownstreamRoute>> CreateRoutes(List<FakeDownstreamRoute> routes)
        => Options.Create(routes);

    private class DisallowNotModifyCachePolicy : IETagCachePolicy
    {
        public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
        {
            context.AllowNotModified = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private class Store : IOutputCacheStore
    {
        private byte[]? _entry;

        public bool WasCallSetAsync { get; private set; }

        public static Store Create(ETagCacheEntry? entry = null)
            => new(entry);

        private Store(ETagCacheEntry? entry = null)
        {
            _entry = entry?.Serialize();
        }

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public ValueTask SetAsync(
            string key,
            byte[] value,
            string[]? tags,
            TimeSpan validFor,
            CancellationToken cancellationToken)
        {
            WasCallSetAsync = true;
            _entry = value;

            return ValueTask.CompletedTask;
        }

        ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_entry);
        }
    }

    private class FakeLogger : ILogger<ETagCachingMiddleware>
    {
        public int ExecuteCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ExecuteCount++;
        }
    }
}
