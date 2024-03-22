using Kros.Ocelot.ETagCaching;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the ETagCacning middleware.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ETag caching to the Ocelot.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The configuration action.</param>
    public static IServiceCollection AddOcelotETagCaching(
        this IServiceCollection services,
        Action<ETagCachingOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ETagCachingMiddleware>();
        services.AddOutputCache();
        services.AddTransient<IETagCachingMiddleware, ETagCachingMiddleware>();

        AddFakeConfiguration(services);

        return services;
    }

    [Obsolete(
        "This can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be release.",
        DiagnosticId = "KO001")]
    private static void AddFakeConfiguration(IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services.Configure<List<FakeDownstreamRoute>>(configuration.GetSection("Routes"));
    }
}
