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

        return services;
    }
}
