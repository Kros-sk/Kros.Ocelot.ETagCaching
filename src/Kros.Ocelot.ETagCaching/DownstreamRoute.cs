namespace Kros.Ocelot.ETagCaching;

// this can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be merged
internal record DownstreamRoute(string Key, string CachePolicy);
