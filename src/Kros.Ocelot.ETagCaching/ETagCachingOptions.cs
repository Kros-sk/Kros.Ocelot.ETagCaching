﻿using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Options for the ETag caching.
/// </summary>
public sealed class ETagCachingOptions
{
    private readonly Dictionary<string, IETagCachePolicy> _policies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IInvalidateCachePolicy> _invalidatePolicies = new(StringComparer.OrdinalIgnoreCase);

    internal static ETagCachingOptions Create()
        => new();

    /// <summary>
    /// Adds a policy to the ETag caching.
    /// </summary>
    /// <param name="policyName">This name shoud be use for downstream path configuration.</param>
    /// <param name="builder">Policy builder.</param>
    /// <param name="excludeDefaultPolicy">If true, default policy will not be added.</param>
    /// <exception cref="ArgumentException">If policy with the same name already exists.</exception>
    public ETagCachingOptions AddPolicy(
        string policyName,
        Action<ETagCachePolicyBuilder> builder,
        bool excludeDefaultPolicy = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName, nameof(policyName));

        if (_policies.ContainsKey(policyName))
        {
            throw new ArgumentException($"Policy with name '{policyName}' already exists.", policyName);
        }

        var policyBuilder = new ETagCachePolicyBuilder(excludeDefaultPolicy);
        builder(policyBuilder);

        _policies.Add(policyName, policyBuilder.Build());

        return this;
    }

    /// <summary>
    /// Adds a default policy to the ETag caching.
    /// </summary>
    /// <param name="policyName">This name shoud be use for downstream path configuration.</param>
    /// <exception cref="ArgumentException">If policy with the same name already exists.</exception>
    public ETagCachingOptions AddDefaultPolicy(string policyName)
        => AddPolicy(policyName, _ => { });

    /// <summary>
    /// Adds a policy to invalidate cache.
    /// </summary>
    /// <param name="policyName">Policy name.</param>
    /// <param name="builder">Policy builder</param>
    /// <exception cref="ArgumentException">If policy with the same name already exists.</exception>
    public ETagCachingOptions AddInvalidatePolicy(
        string policyName,
        Action<InvalidateCachePolicyBuilder> builder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName, nameof(policyName));

        if (_invalidatePolicies.ContainsKey(policyName))
        {
            throw new ArgumentException($"Policy with name '{policyName}' already exists.", policyName);
        }

        var policyBuilder = new InvalidateCachePolicyBuilder();
        builder(policyBuilder);

        _invalidatePolicies.Add(policyName, policyBuilder.Build());

        return this;
    }

    /// <summary>
    /// Get policy.
    /// </summary>
    /// <param name="policyName">Policy name.</param>
    public IETagCachePolicy GetPolicy(string policyName)
        => _policies.TryGetValue(policyName, out var policy)
        ? policy
        : EmptyPolicy.Instance;

    /// <summary>
    /// Get invalidate policy.
    /// </summary>
    /// <param name="policyName">Policy name.</param>
    public IInvalidateCachePolicy GetInvalidatePolicy(string policyName)
        => _invalidatePolicies.TryGetValue(policyName, out var policy)
        ? policy
        : InvalidateEmptyPolicy.Instance;
}
