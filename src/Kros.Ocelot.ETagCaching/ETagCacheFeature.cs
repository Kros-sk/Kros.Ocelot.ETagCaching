namespace Kros.Ocelot.ETagCaching;

/// <summary>
/// Feature for ETag cache.
/// </summary>
public sealed class ETagCacheFeature
{
    internal ETagCacheFeature(ETagCacheContext eTagCacheContext)
    {
        ETagCacheContext = eTagCacheContext;
    }

    /// <summary>
    /// Gets the <see cref="ETagCacheContext"/>.
    /// </summary>
    public ETagCacheContext ETagCacheContext { get; }
}
