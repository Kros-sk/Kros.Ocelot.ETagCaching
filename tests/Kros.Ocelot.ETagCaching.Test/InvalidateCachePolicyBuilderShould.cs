using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test;

public class InvalidateCachePolicyBuilderShould
{
    [Fact]
    public void BuildEmptyPolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder.Build();

        Assert.IsType<InvalidateEmptyPolicy>(policy);
    }

    [Fact]
    public void BuildTagTemplatesPolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder.TagTemplates("products").Build();

        Assert.IsType<InvalidateDefaultPolicy>(policy);
    }

    [Fact]
    public void BuildCompositePolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder
            .AddPolicy<InvalidateEmptyPolicy>()
            .AddPolicy<FakePolicy>()
            .Build();

        Assert.IsType<InvalidateCompositePolicy>(policy);
    }

    private class FakePolicy : IInvalidateCachePolicy
    {
        public ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
