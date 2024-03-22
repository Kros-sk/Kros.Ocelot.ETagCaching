using Kros.Ocelot.ETagCaching.Policies;
using NetArchTest.Rules;

namespace Kros.Ocelot.ETagCaching.Test.Policies;

public class ArchTests
{
    [Fact]
    public void PoliciesNamespaceClassesShouldBeInternal()
    {
        var result = Types.InAssembly(typeof(DefaultPolicy).Assembly)
            .That()
                .ResideInNamespace("Kros.Ocelot.ETagCaching.Policies")
            .Should()
                .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Policies namespace should contain only internal classes");
    }
}
