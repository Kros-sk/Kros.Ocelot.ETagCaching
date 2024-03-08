using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kros.Ocelot.ETagCaching;

internal sealed class ETagCacheEntry(EntityTagHeaderValue eTag, Dictionary<string, object?> extraProps)
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    [JsonConstructor]
    public ETagCacheEntry(string eTag, Dictionary<string, object?> extraProps)
        : this(new EntityTagHeaderValue(eTag), extraProps)
    {
    }

    public static ETagCacheEntry? Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<ETagCacheEntry>(json, _jsonSerializerOptions);
    }

    public byte[] Serialize()
    {
        var json = JsonSerializer.Serialize(this);
        return Encoding.UTF8.GetBytes(json);
    }

    public string ETag { get; } = eTag.ToString();

    public Dictionary<string, object?> ExtraProps { get; } = extraProps;
}
