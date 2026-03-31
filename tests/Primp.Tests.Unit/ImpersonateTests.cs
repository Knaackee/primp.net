namespace Primp.Tests.Unit;

public class ImpersonateTests
{
    [Fact]
    public void AllValues_AreDefined()
    {
        // Ensure all enum values are valid
        foreach (var value in Enum.GetValues<Impersonate>())
        {
            Assert.True(Enum.IsDefined(value), $"Impersonate.{value} is not defined");
        }
    }

    [Fact]
    public void ExpectedValues_Exist()
    {
        // Verify key browser variants exist
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Chrome144));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Chrome146));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Safari185));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Edge146));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Firefox148));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Opera129));
        Assert.True(Enum.IsDefined<Impersonate>(Impersonate.Random));
    }
}
