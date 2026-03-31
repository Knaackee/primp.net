using Microsoft.Win32.SafeHandles;

namespace Primp.Interop;

/// <summary>
/// SafeHandle for a native primp client pointer.
/// Ensures the client is freed even if Dispose is not called.
/// </summary>
internal sealed class ClientSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public ClientSafeHandle() : base(ownsHandle: true) { }

    public ClientSafeHandle(nint handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.ClientFree(handle);
        return true;
    }
}
