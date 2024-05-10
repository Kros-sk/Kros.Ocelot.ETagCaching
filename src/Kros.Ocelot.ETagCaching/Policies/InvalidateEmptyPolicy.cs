
namespace Kros.Ocelot.ETagCaching.Policies;

internal class InvalidateEmptyPolicy : IInvalidateCachePolicy
{
    public static IInvalidateCachePolicy Instance { get; } = new InvalidateEmptyPolicy();

    // Stryker disable Block: results in an equivalent mutation
    public ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
