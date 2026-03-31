using Microsoft.Win32.SafeHandles;

namespace Primp.Interop;

/// <summary>
/// SafeHandle for a native primp response pointer.
/// Ensures the response is freed even if Dispose is not called.
/// </summary>
internal sealed class ResponseSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public ResponseSafeHandle() : base(ownsHandle: true) { }

    public ResponseSafeHandle(nint handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.ResponseFree(handle);
        return true;
    }
}
