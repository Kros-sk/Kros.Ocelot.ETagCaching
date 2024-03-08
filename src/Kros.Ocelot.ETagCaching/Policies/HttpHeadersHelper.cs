namespace Kros.Ocelot.ETagCaching.Policies;

internal static class HttpHeadersHelper
{
    public const string CacheControlHeaderName = "Cache-Control";
    public const string NoCacheHeaderValue = "no-cache";
    public const string NoStoreHeaderValue = "no-store";
    public const string ETagHeaderName = "ETag";
    public const string IfNoneMatchHeaderName = "If-None-Match";
}
