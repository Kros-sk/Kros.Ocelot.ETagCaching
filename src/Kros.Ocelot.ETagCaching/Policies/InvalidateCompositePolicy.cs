namespace Kros.Ocelot.ETagCaching.Policies;

internal class InvalidateCompositePolicy(List<IInvalidateCachePolicy> policies) : IInvalidateCachePolicy
{
    private readonly List<IInvalidateCachePolicy> _policies = policies;

    public async ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken = default)
    {
        foreach (var policy in _policies)
        {
            await policy.InvalidateCacheAsync(context, cancellationToken);
        }
    }
}
