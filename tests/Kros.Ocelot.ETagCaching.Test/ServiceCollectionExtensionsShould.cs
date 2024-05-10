using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kros.Ocelot.ETagCaching.Test;

public class ServiceCollectionExtensionsShould
{
    [Fact]
    public async Task AddETagCaching()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddOcelotETagCaching(conf =>
        {
            conf.AddPolicy("getAllProduct", builder =>
            {
                builder.Expire(TimeSpan.FromHours(8));
                builder.TagTemplates("product:{tenantId}", "product:{tenantId}:{id}", "all:{tenantId}");
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ETagCachingOptions>>();

        var policy = options.Value.GetPolicy("getAllProduct");

        var context = ETagCacheContextFactory.CreateContext();

        await policy!.CacheETagAsync(context, default);

        context.ETagExpirationTimeSpan.Should().Be(TimeSpan.FromHours(8));
        context.EnableETagCache.Should().BeTrue();
    }

    [Fact]
    public async Task AddETagCachingWithoutDefaultPolicy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddOcelotETagCaching(conf =>
        {
            conf.AddPolicy("getAllProduct",
                builder => builder.Expire(TimeSpan.FromHours(8)),
                excludeDefaultPolicy: true);
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ETagCachingOptions>>();

        var policy = options.Value.GetPolicy("getAllProduct");
        var context = ETagCacheContextFactory.CreateContext();
        context.EnableETagCache = false;

        await policy.CacheETagAsync(context, default);

        context.EnableETagCache.Should().BeFalse();
    }

    [Fact]
    public void ThrowExceptionWhenPolicyWithTheSameNameIsAdded()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Action? act = null;

        services.AddOcelotETagCaching(conf =>
        {
            conf.AddPolicy("getAllProduct", _ => { });
            act = () => conf.AddPolicy("getAllProduct", _ => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ETagCachingOptions>>().Value;
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Policy with name 'getAllProduct' already exists. (Parameter 'getAllProduct')");
    }

    [Fact]
    public async Task AddInvalidatePolicy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddOcelotETagCaching(conf =>
        {
            conf.AddInvalidatePolicy("invalidateProduct", builder
                => builder.TagTemplates("product:{tenantId}", "product:{tenantId}:{id}"));
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ETagCachingOptions>>().Value;
        var policy = options.GetInvalidatePolicy("invalidateProduct");

        var context = InvalidateCacheContextFactory.CreateContext();
        await policy.InvalidateCacheAsync(context, default);

        context.Tags.Should().BeEquivalentTo(["product:1", "product:1:2"]);
    }

    [Fact]
    public void ThrowExceptionWhenInvalidatePolicyWithTheSameNameIsAdded()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Action? act = null;

        services.AddOcelotETagCaching(conf =>
        {
            conf.AddInvalidatePolicy("invalidateProduct", _ => { });
            act = () => conf.AddInvalidatePolicy("invalidateProduct", _ => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ETagCachingOptions>>().Value;
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Policy with name 'invalidateProduct' already exists. (Parameter 'invalidateProduct')");
    }
}
