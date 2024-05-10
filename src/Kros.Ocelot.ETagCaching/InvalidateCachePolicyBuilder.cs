using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Provides helper methods to create <see cref="IInvalidateCachePolicy"/>.
/// </summary>
public sealed class InvalidateCachePolicyBuilder
{
    private readonly List<IInvalidateCachePolicy> _policies = [];

    /// <summary>
    /// Adds a policy.
    /// </summary>
    /// <param name="policy">Policy</param>
    public InvalidateCachePolicyBuilder AddPolicy(IInvalidateCachePolicy policy)
    {
        _policies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds a policy.
    /// </summary>
    /// <typeparam name="TPolicy">Policy type.</typeparam>
    public InvalidateCachePolicyBuilder AddPolicy<TPolicy>()
           where TPolicy : IInvalidateCachePolicy, new()
    {
        AddPolicy(new TPolicy());
        return this;
    }

    /// <summary>
    /// Adds a policy for invalidating cache based on the tag templates.
    /// </summary>
    /// <param name="tagTemplates">The tag templates of the cached response</param>
    public InvalidateCachePolicyBuilder TagTemplates(params string[] tagTemplates)
    {
        AddPolicy(new InvalidateDefaultPolicy(tagTemplates));
        return this;
    }

    /// <summary>
    /// Builds the <see cref="IInvalidateCachePolicy"/>.
    /// </summary>
    public IInvalidateCachePolicy Build()
        => _policies.Count switch
        {
            0 => InvalidateEmptyPolicy.Instance,
            1 => _policies[0],
            _ => new InvalidateCompositePolicy(_policies)
        };
}
