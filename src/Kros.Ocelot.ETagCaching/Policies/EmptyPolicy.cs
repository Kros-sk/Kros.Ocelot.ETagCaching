﻿namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class EmptyPolicy : IETagCachePolicy
{
    public static IETagCachePolicy Instance { get; } = new EmptyPolicy();

    // Stryker disable Block: results in an equivalent mutation
    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
