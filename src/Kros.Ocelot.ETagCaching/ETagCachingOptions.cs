using Kros.Ocelot.ETagCaching;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Options for the ETag caching.
/// </summary>
public sealed class ETagCachingOptions
{
    private readonly Dictionary<string, IETagCachePolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

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
    /// Tries to get a policy.
    /// </summary>
    /// <param name="policyName">Policy name.</param>
    /// <param name="policy">Policy if exist.</param>
    public bool TryGetPolicy(string policyName, out IETagCachePolicy? policy)
        => _policies.TryGetValue(policyName, out policy);
}
