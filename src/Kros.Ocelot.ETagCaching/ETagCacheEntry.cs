using Microsoft.Net.Http.Headers;

namespace Kros.Ocelot.ETagCaching;

internal sealed class ETagCacheEntry(EntityTagHeaderValue eTag, DateTimeOffset lastModified)
{
    public string ETag { get; } = eTag.ToString();

    public DateTimeOffset LastModified { get; } = lastModified;

    public Dictionary<string, object?> ExtraProps { get; } = [];
}
