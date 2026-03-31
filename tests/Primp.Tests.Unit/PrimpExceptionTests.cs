namespace Primp.Tests.Unit;

public class PrimpExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        var ex = new PrimpException("test error");
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void Constructor_SetsNativeErrorCode()
    {
        var ex = new PrimpException("test error", nativeErrorCode: -1);
        Assert.Equal(-1, ex.NativeErrorCode);
    }

    [Fact]
    public void Constructor_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new PrimpException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void DefaultNativeErrorCode_IsZero()
    {
        var ex = new PrimpException("test");
        Assert.Equal(0, ex.NativeErrorCode);
    }
}
