using System.Runtime.InteropServices;

namespace Primp.Interop;

internal static partial class NativeMethods
{
    private const string Lib = "primp_ffi";

    // =========================================================================
    // Error
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_last_error")]
    private static partial nint PrimpLastError();

    internal static string? GetLastError()
    {
        var ptr = PrimpLastError();
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    // =========================================================================
    // Builder
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_builder_new")]
    internal static partial nint BuilderNew();

    [LibraryImport(Lib, EntryPoint = "primp_builder_free")]
    internal static partial void BuilderFree(nint builder);

    [LibraryImport(Lib, EntryPoint = "primp_builder_impersonate", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int BuilderImpersonate(nint builder, string name);

    [LibraryImport(Lib, EntryPoint = "primp_builder_impersonate_os", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int BuilderImpersonateOs(nint builder, string name);

    [LibraryImport(Lib, EntryPoint = "primp_builder_timeout_ms")]
    internal static partial int BuilderTimeoutMs(nint builder, ulong ms);

    [LibraryImport(Lib, EntryPoint = "primp_builder_connect_timeout_ms")]
    internal static partial int BuilderConnectTimeoutMs(nint builder, ulong ms);

    [LibraryImport(Lib, EntryPoint = "primp_builder_proxy", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int BuilderProxy(nint builder, string url);

    [LibraryImport(Lib, EntryPoint = "primp_builder_cookie_store")]
    internal static partial int BuilderCookieStore(nint builder, int enabled);

    [LibraryImport(Lib, EntryPoint = "primp_builder_https_only")]
    internal static partial int BuilderHttpsOnly(nint builder, int enabled);

    [LibraryImport(Lib, EntryPoint = "primp_builder_danger_accept_invalid_certs")]
    internal static partial int BuilderDangerAcceptInvalidCerts(nint builder, int enabled);

    [LibraryImport(Lib, EntryPoint = "primp_builder_follow_redirects")]
    internal static partial int BuilderFollowRedirects(nint builder, int enabled);

    [LibraryImport(Lib, EntryPoint = "primp_builder_max_redirects")]
    internal static partial int BuilderMaxRedirects(nint builder, uint max);

    [LibraryImport(Lib, EntryPoint = "primp_builder_default_headers_json", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int BuilderDefaultHeadersJson(nint builder, string json);

    [LibraryImport(Lib, EntryPoint = "primp_builder_build")]
    internal static partial int BuilderBuild(nint builder, out nint client);

    // =========================================================================
    // Request
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_request", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int Request(
        nint client,
        string method,
        string url,
        nint bodyPtr,
        nuint bodyLen,
        string? headersJson,
        out nint response);

    // =========================================================================
    // Response
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_response_status")]
    internal static partial ushort ResponseStatus(nint response);

    [LibraryImport(Lib, EntryPoint = "primp_response_body")]
    internal static partial void ResponseBody(nint response, out nint ptr, out nuint len);

    [LibraryImport(Lib, EntryPoint = "primp_response_headers")]
    private static partial nint ResponseHeadersPtr(nint response);

    internal static string? ResponseHeaders(nint response)
    {
        var ptr = ResponseHeadersPtr(response);
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    [LibraryImport(Lib, EntryPoint = "primp_response_url")]
    private static partial nint ResponseUrlPtr(nint response);

    internal static string? ResponseUrl(nint response)
    {
        var ptr = ResponseUrlPtr(response);
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    // =========================================================================
    // Cleanup
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_client_free")]
    internal static partial void ClientFree(nint client);

    [LibraryImport(Lib, EntryPoint = "primp_response_free")]
    internal static partial void ResponseFree(nint response);

    // =========================================================================
    // Version
    // =========================================================================

    [LibraryImport(Lib, EntryPoint = "primp_ffi_version")]
    private static partial nint FfiVersionPtr();

    internal static string? FfiVersion()
    {
        var ptr = FfiVersionPtr();
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }
}
