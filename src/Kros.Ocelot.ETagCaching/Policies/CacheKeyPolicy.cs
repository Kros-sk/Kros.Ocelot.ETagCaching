﻿namespace Kros.Ocelot.ETagCaching.Policies;

internal sealed class CacheKeyPolicy : IETagCachePolicy
{
    public ValueTask CacheETagAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask ServeNotModifiedAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask ServeDownstreamResponseAsync(ETagCacheContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}