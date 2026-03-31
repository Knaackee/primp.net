namespace Primp;

/// <summary>
/// Browser profiles that can be impersonated.
/// The string representation (e.g. "chrome_146") is passed to the native layer.
/// </summary>
public enum Impersonate
{
    Chrome144,
    Chrome145,
    Chrome146,
    Chrome,

    Safari185,
    Safari26,
    Safari263,
    Safari,

    Edge144,
    Edge145,
    Edge146,
    Edge,

    Firefox140,
    Firefox146,
    Firefox147,
    Firefox148,
    Firefox,

    Opera126,
    Opera127,
    Opera128,
    Opera129,
    Opera,

    Random,
}

internal static class ImpersonateExtensions
{
    internal static string ToNativeString(this Impersonate value) => value switch
    {
        Impersonate.Chrome144 => "chrome_v144",
        Impersonate.Chrome145 => "chrome_v145",
        Impersonate.Chrome146 => "chrome_v146",
        Impersonate.Chrome => "chrome",
        Impersonate.Safari185 => "safari_v18_5",
        Impersonate.Safari26 => "safari_v26",
        Impersonate.Safari263 => "safari_v26_3",
        Impersonate.Safari => "safari",
        Impersonate.Edge144 => "edge_v144",
        Impersonate.Edge145 => "edge_v145",
        Impersonate.Edge146 => "edge_v146",
        Impersonate.Edge => "edge",
        Impersonate.Firefox140 => "firefox_v140",
        Impersonate.Firefox146 => "firefox_v146",
        Impersonate.Firefox147 => "firefox_v147",
        Impersonate.Firefox148 => "firefox_v148",
        Impersonate.Firefox => "firefox",
        Impersonate.Opera126 => "opera_v126",
        Impersonate.Opera127 => "opera_v127",
        Impersonate.Opera128 => "opera_v128",
        Impersonate.Opera129 => "opera_v129",
        Impersonate.Opera => "opera",
        Impersonate.Random => "random",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
