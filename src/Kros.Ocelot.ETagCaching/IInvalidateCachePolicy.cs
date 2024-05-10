namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Interface for policy that can invalidate cache.
/// </summary>
public interface IInvalidateCachePolicy
{
    /// <summary>
    /// Invalidates cache based on the context.
    /// </summary>
    /// <param name="context">The context for cache invalidation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken);
}
