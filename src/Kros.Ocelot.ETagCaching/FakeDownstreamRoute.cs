namespace Kros.Ocelot.ETagCaching;

[Obsolete(
    "This can be removed when issue https://github.com/ThreeMammals/Ocelot/pull/1843 will be release.",
    DiagnosticId = "KO001")]
internal record FakeDownstreamRoute(string Key, string? CachePolicy = null, HashSet<string>? InvalidateCache = null);
