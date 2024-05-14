using Kros.Ocelot.ETagCaching.Policies;

namespace Kros.Ocelot.ETagCaching.Test;

public class InvalidateCachePolicyBuilderShould
{
    [Fact]
    public void BuildEmptyPolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder.Build();

        policy.Should().BeOfType<InvalidateEmptyPolicy>();
    }

    [Fact]
    public void BuildTagTemplatesPolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder.TagTemplates("products").Build();

        policy.Should().BeOfType<InvalidateDefaultPolicy>();
    }

    [Fact]
    public void BuildCompositePolicy()
    {
        var builder = new InvalidateCachePolicyBuilder();
        var policy = builder
            .AddPolicy<InvalidateEmptyPolicy>()
            .AddPolicy<FakePolicy>()
            .Build();

        policy.Should().BeOfType<InvalidateCompositePolicy>();
    }

    private class FakePolicy : IInvalidateCachePolicy
    {
        public ValueTask InvalidateCacheAsync(InvalidateCacheContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
