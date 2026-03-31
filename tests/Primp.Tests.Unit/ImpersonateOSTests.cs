namespace Primp.Tests.Unit;

public class ImpersonateOSTests
{
    [Fact]
    public void AllValues_AreDefined()
    {
        foreach (var value in Enum.GetValues<ImpersonateOS>())
        {
            Assert.True(Enum.IsDefined(value), $"ImpersonateOS.{value} is not defined");
        }
    }

    [Fact]
    public void ExpectedValues_Exist()
    {
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.Android));
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.iOS));
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.Linux));
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.MacOS));
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.Windows));
        Assert.True(Enum.IsDefined<ImpersonateOS>(ImpersonateOS.Random));
    }
}
