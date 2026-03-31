namespace Primp;

/// <summary>
/// Exception thrown when a primp native operation fails.
/// </summary>
public class PrimpException : Exception
{
    /// <summary>
    /// The native error code returned by the FFI layer.
    /// </summary>
    public int NativeErrorCode { get; }

    public PrimpException(string message) : base(message) { }

    public PrimpException(string message, int nativeErrorCode)
        : base(message)
    {
        NativeErrorCode = nativeErrorCode;
    }

    public PrimpException(string message, Exception innerException)
        : base(message, innerException) { }

    internal static void ThrowIfError(int errorCode)
    {
        if (errorCode != 0)
        {
            var nativeMessage = Interop.NativeMethods.GetLastError();
            var message = string.IsNullOrEmpty(nativeMessage)
                ? $"Native primp operation failed with error code {errorCode}"
                : nativeMessage;
            throw new PrimpException(message, errorCode);
        }
    }
}
