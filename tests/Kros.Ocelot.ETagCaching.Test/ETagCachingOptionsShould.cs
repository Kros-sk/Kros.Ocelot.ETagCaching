namespace Kros.Ocelot.ETagCaching.Test;

public class ETagCachingOptionsShould
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ThrowExceptionIfNameIsEmptyOrWhiteSpace(string name)
    {
        var options = new ETagCachingOptions();

        var action = () => options.AddPolicy(name, (_) => { });

        action.Should().Throw<ArgumentException>();
    }
}
