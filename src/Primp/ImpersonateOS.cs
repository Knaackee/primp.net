namespace Primp;

/// <summary>
/// Operating system to impersonate for browser fingerprinting.
/// </summary>
public enum ImpersonateOS
{
    Android,
    iOS,
    Linux,
    MacOS,
    Windows,
    Random,
}

internal static class ImpersonateOSExtensions
{
    internal static string ToNativeString(this ImpersonateOS value) => value switch
    {
        ImpersonateOS.Android => "android",
        ImpersonateOS.iOS => "ios",
        ImpersonateOS.Linux => "linux",
        ImpersonateOS.MacOS => "macos",
        ImpersonateOS.Windows => "windows",
        ImpersonateOS.Random => "random",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
