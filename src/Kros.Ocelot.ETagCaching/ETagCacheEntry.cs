namespace Kros.Ocelot.ETagCaching;

internal sealed class ETagCacheEntry(string eTag, DateTimeOffset lastModified)
{
    public string ETag { get; } = eTag;

    public DateTimeOffset LastModified { get; } = lastModified;

    public Dictionary<string, object?> ExtraProps { get; } = [];
}
