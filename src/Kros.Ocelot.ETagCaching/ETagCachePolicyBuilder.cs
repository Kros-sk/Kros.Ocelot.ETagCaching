﻿using Kros.Ocelot.ETagCaching.Policies;
using Microsoft.Net.Http.Headers;
using Ocelot.Request.Middleware;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Provides helper methods to create custom policies.
/// </summary>
public sealed class ETagCachePolicyBuilder
{
    private readonly List<IETagCachePolicy> _policies = [];

    internal ETagCachePolicyBuilder(bool excludeDefaultPolicy = false)
    {
        if (!excludeDefaultPolicy)
        {
            _policies.Add(DefaultPolicy.Instance);
        }
    }

    /// <summary>
    /// Adds a policy.
    /// </summary>
    /// <param name="policy">Policy</param>
    public ETagCachePolicyBuilder AddPolicy(IETagCachePolicy policy)
    {
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a policy.
    /// </summary>
    /// <typeparam name="TPolicy">Policy type.</typeparam>
    public ETagCachePolicyBuilder AddPolicy<TPolicy>()
        where TPolicy : IETagCachePolicy, new()
    {
        AddPolicy(new TPolicy());
        return this;
    }

    /// <summary>
    /// Adds a policy to change the cached response expiration.
    /// </summary>
    /// <param name="expiration">The expiration of the cached reponse.</param>
    public ETagCachePolicyBuilder Expire(TimeSpan expiration)
    {
        AddPolicy(new ExpirationPolicy(expiration));
        return this;
    }

    /// <summary>
    /// Adds a policy add the tag templates to the cached response.
    /// </summary>
    /// <param name="tagTemplates">The tag templates of the cached response</param>
    public ETagCachePolicyBuilder TagTemplates(params string[] tagTemplates)
    {
        AddPolicy(new TagTemplatesPolicy(tagTemplates));
        return this;
    }

    /// <summary>
    /// Add a policy to change the cache control header of the cached response.
    /// </summary>
    /// <param name="cacheControl">Cache control.</param>
    public ETagCachePolicyBuilder CacheControl(CacheControlHeaderValue cacheControl)
    {
        AddPolicy(new CacheControlPolicy(cacheControl));
        return this;
    }

    /// <summary>
    /// Add a policy to change the cache key of the cached response.
    /// </summary>
    /// <param name="keyGenerator">Cache key generator.</param>
    public ETagCachePolicyBuilder CacheKey(Func<DownstreamRequest, string> keyGenerator)
    {
        AddPolicy(new CacheKeyPolicy(keyGenerator));
        return this;
    }

    /// <summary>
    /// Add a policy to change the ETag of the cached response.
    /// </summary>
    /// <param name="etagGenerator">ETag generator.</param>
    public ETagCachePolicyBuilder ETag(Func<ETagCacheContext, EntityTagHeaderValue> etagGenerator)
    {
        AddPolicy(new ETagPolicy(etagGenerator));
        return this;
    }

    /// <summary>
    /// Add a policy to change the status code of the cached response.
    /// </summary>
    /// <param name="statusCode">Status code for response serve from cache.</param>
    public ETagCachePolicyBuilder StatusCode(int statusCode)
    {
        AddPolicy(new StatusCodePolicy(statusCode));
        return this;
    }

    /// <summary>
    /// Add a policy which add extra properties to the cache entry.
    /// </summary>
    /// <param name="extraProps">Configure extra properties.</param>
    public ETagCachePolicyBuilder CacheEntryExtraProp(Action<Dictionary<string, object>> extraProps)
    {
        var props = new Dictionary<string, object>();
        extraProps(props);
        AddPolicy(new CacheEntryExtraPropsPolicy(props));

        return this;
    }

    /// <summary>
    /// Clears the policies and adds one preventing any caching logic to happen.
    /// </summary>
    public ETagCachePolicyBuilder NoCache()
    {
        _policies.Clear();
        AddPolicy(EnableCachePolicy.Disabled);

        return this;
    }

    /// <summary>
    /// Enables caching for the current request if not already enabled.
    /// </summary>
    /// <remarks>If no custom policy is added, the <see cref="DefaultPolicy"/> is already "enabled".</remarks>
    public ETagCachePolicyBuilder Cache()
    {
        if (_policies.Count != 1 || _policies[0] != DefaultPolicy.Instance)
        {
            AddPolicy(EnableCachePolicy.Enabled);
        }

        return this;
    }

    internal IETagCachePolicy Build()
        => _policies.Count switch
        {
            0 => EmptyPolicy.Instance,
            1 => _policies[0],
            _ => new CompositePolicy(_policies)
        };
}
