//! C-FFI bridge for the primp HTTP client library.
//!
//! This crate provides a thin C-compatible API over the primp Rust library,
//! allowing .NET (and other languages) to use primp via P/Invoke.
//!
//! **No logic is reimplemented here.** This is pure type conversion
//! (Rust types ↔ C types) and `runtime.block_on()` for async bridging.

use std::ffi::{c_char, CStr, CString};
use std::ptr;
use std::sync::OnceLock;
use tokio::runtime::Runtime;

static RUNTIME: OnceLock<Runtime> = OnceLock::new();

fn rt() -> &'static Runtime {
    RUNTIME.get_or_init(|| {
        Runtime::new().expect("Failed to create Tokio runtime for primp-ffi")
    })
}

// =============================================================================
// Error handling
// =============================================================================

/// Thread-local storage for the last error message.
thread_local! {
    static LAST_ERROR: std::cell::RefCell<Option<CString>> = const { std::cell::RefCell::new(None) };
}

fn set_last_error(msg: &str) {
    LAST_ERROR.with(|e| {
        *e.borrow_mut() = CString::new(msg).ok();
    });
}

/// Returns a pointer to the last error message, or null if none.
/// The returned pointer is valid until the next FFI call on the same thread.
#[no_mangle]
pub extern "C" fn primp_last_error() -> *const c_char {
    LAST_ERROR.with(|e| {
        e.borrow()
            .as_ref()
            .map(|s| s.as_ptr())
            .unwrap_or(ptr::null())
    })
}

// =============================================================================
// Opaque handle types
// =============================================================================

/// Opaque builder handle wrapping primp::ClientBuilder and collected options.
pub struct FfiClientBuilder {
    impersonate: Option<String>,
    impersonate_os: Option<String>,
    timeout_ms: Option<u64>,
    connect_timeout_ms: Option<u64>,
    proxy: Option<String>,
    cookie_store: Option<bool>,
    https_only: Option<bool>,
    danger_accept_invalid_certs: Option<bool>,
    follow_redirects: Option<bool>,
    max_redirects: Option<u32>,
    default_headers_json: Option<String>,
}

/// Opaque client handle wrapping primp::Client.
pub struct FfiClient {
    inner: primp::Client,
}

/// FFI-friendly response that owns all its data.
pub struct FfiResponse {
    status: u16,
    headers_json: CString,
    body: Vec<u8>,
    url: CString,
}

// =============================================================================
// Client Builder
// =============================================================================

#[no_mangle]
pub extern "C" fn primp_builder_new() -> *mut FfiClientBuilder {
    Box::into_raw(Box::new(FfiClientBuilder {
        impersonate: None,
        impersonate_os: None,
        timeout_ms: None,
        connect_timeout_ms: None,
        proxy: None,
        cookie_store: None,
        https_only: None,
        danger_accept_invalid_certs: None,
        follow_redirects: None,
        max_redirects: None,
        default_headers_json: None,
    }))
}

#[no_mangle]
pub extern "C" fn primp_builder_free(builder: *mut FfiClientBuilder) {
    if !builder.is_null() {
        unsafe { drop(Box::from_raw(builder)) };
    }
}

#[no_mangle]
pub extern "C" fn primp_builder_impersonate(
    builder: *mut FfiClientBuilder,
    name: *const c_char,
) -> i32 {
    if builder.is_null() || name.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    let name = match unsafe { CStr::from_ptr(name) }.to_str() {
        Ok(s) => s.to_string(),
        Err(_) => {
            set_last_error("invalid UTF-8 in browser name");
            return -1;
        }
    };
    unsafe { (*builder).impersonate = Some(name) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_impersonate_os(
    builder: *mut FfiClientBuilder,
    name: *const c_char,
) -> i32 {
    if builder.is_null() || name.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    let name = match unsafe { CStr::from_ptr(name) }.to_str() {
        Ok(s) => s.to_string(),
        Err(_) => {
            set_last_error("invalid UTF-8 in OS name");
            return -1;
        }
    };
    unsafe { (*builder).impersonate_os = Some(name) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_timeout_ms(builder: *mut FfiClientBuilder, ms: u64) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).timeout_ms = Some(ms) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_connect_timeout_ms(
    builder: *mut FfiClientBuilder,
    ms: u64,
) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).connect_timeout_ms = Some(ms) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_proxy(
    builder: *mut FfiClientBuilder,
    url: *const c_char,
) -> i32 {
    if builder.is_null() || url.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    let url = match unsafe { CStr::from_ptr(url) }.to_str() {
        Ok(s) => s.to_string(),
        Err(_) => {
            set_last_error("invalid UTF-8 in proxy URL");
            return -1;
        }
    };
    unsafe { (*builder).proxy = Some(url) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_cookie_store(builder: *mut FfiClientBuilder, enabled: i32) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).cookie_store = Some(enabled != 0) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_https_only(builder: *mut FfiClientBuilder, enabled: i32) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).https_only = Some(enabled != 0) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_danger_accept_invalid_certs(
    builder: *mut FfiClientBuilder,
    enabled: i32,
) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).danger_accept_invalid_certs = Some(enabled != 0) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_follow_redirects(
    builder: *mut FfiClientBuilder,
    enabled: i32,
) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).follow_redirects = Some(enabled != 0) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_max_redirects(builder: *mut FfiClientBuilder, max: u32) -> i32 {
    if builder.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    unsafe { (*builder).max_redirects = Some(max) };
    0
}

#[no_mangle]
pub extern "C" fn primp_builder_default_headers_json(
    builder: *mut FfiClientBuilder,
    json: *const c_char,
) -> i32 {
    if builder.is_null() || json.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }
    let json = match unsafe { CStr::from_ptr(json) }.to_str() {
        Ok(s) => s.to_string(),
        Err(_) => {
            set_last_error("invalid UTF-8 in headers JSON");
            return -1;
        }
    };
    unsafe { (*builder).default_headers_json = Some(json) };
    0
}

/// Builds the client from the builder. Consumes the builder.
/// On success, writes the client pointer to `out_client` and returns 0.
/// On failure, returns a negative error code. Use `primp_last_error()` for details.
#[no_mangle]
pub extern "C" fn primp_builder_build(
    builder: *mut FfiClientBuilder,
    out_client: *mut *mut FfiClient,
) -> i32 {
    if builder.is_null() || out_client.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }

    let opts = unsafe { *Box::from_raw(builder) };
    let mut b = primp::Client::builder();

    // Impersonate
    if let Some(ref name) = opts.impersonate {
        let imp = match parse_impersonate(name) {
            Some(v) => v,
            None => {
                set_last_error(&format!("unknown impersonate value: {name}"));
                return -2;
            }
        };
        b = b.impersonate(imp);
    }

    // Impersonate OS
    if let Some(ref name) = opts.impersonate_os {
        let os = match parse_impersonate_os(name) {
            Some(v) => v,
            None => {
                set_last_error(&format!("unknown impersonate_os value: {name}"));
                return -2;
            }
        };
        b = b.impersonate_os(os);
    }

    // Timeout
    if let Some(ms) = opts.timeout_ms {
        b = b.timeout(std::time::Duration::from_millis(ms));
    }

    // Connect timeout
    if let Some(ms) = opts.connect_timeout_ms {
        b = b.connect_timeout(std::time::Duration::from_millis(ms));
    }

    // Proxy
    if let Some(ref url) = opts.proxy {
        match primp::Proxy::all(url) {
            Ok(proxy) => {
                b = b.proxy(proxy);
            }
            Err(e) => {
                set_last_error(&format!("invalid proxy URL: {e}"));
                return -3;
            }
        }
    }

    // Cookie store
    if let Some(enabled) = opts.cookie_store {
        b = b.cookie_store(enabled);
    }

    // HTTPS only
    if let Some(enabled) = opts.https_only {
        b = b.https_only(enabled);
    }

    // Accept invalid certs
    if let Some(enabled) = opts.danger_accept_invalid_certs {
        b = b.danger_accept_invalid_certs(enabled);
    }

    // Redirect policy
    if let Some(enabled) = opts.follow_redirects {
        if !enabled {
            b = b.redirect(primp::redirect::Policy::none());
        } else if let Some(max) = opts.max_redirects {
            b = b.redirect(primp::redirect::Policy::limited(max as usize));
        }
    } else if let Some(max) = opts.max_redirects {
        b = b.redirect(primp::redirect::Policy::limited(max as usize));
    }

    // Default headers
    if let Some(ref json) = opts.default_headers_json {
        let map: std::collections::HashMap<String, String> = match serde_json::from_str(json) {
            Ok(m) => m,
            Err(e) => {
                set_last_error(&format!("invalid default_headers JSON: {e}"));
                return -4;
            }
        };
        let mut headers = primp::header::HeaderMap::new();
        for (k, v) in &map {
            let name = match primp::header::HeaderName::from_bytes(k.as_bytes()) {
                Ok(n) => n,
                Err(e) => {
                    set_last_error(&format!("invalid header name '{k}': {e}"));
                    return -4;
                }
            };
            let value = match primp::header::HeaderValue::from_str(v) {
                Ok(v) => v,
                Err(e) => {
                    set_last_error(&format!("invalid header value for '{k}': {e}"));
                    return -4;
                }
            };
            headers.insert(name, value);
        }
        b = b.default_headers(headers);
    }

    match b.build() {
        Ok(client) => {
            let ffi_client = Box::new(FfiClient { inner: client });
            unsafe { *out_client = Box::into_raw(ffi_client) };
            0
        }
        Err(e) => {
            set_last_error(&format!("failed to build client: {e}"));
            -5
        }
    }
}

// =============================================================================
// Enum parsing helpers (maps C strings → primp enums)
// This is the ONLY place that needs updating when primp adds new variants.
// =============================================================================

fn parse_impersonate(name: &str) -> Option<primp::Impersonate> {
    Some(match name {
        "chrome_v144" => primp::Impersonate::ChromeV144,
        "chrome_v145" => primp::Impersonate::ChromeV145,
        "chrome_v146" => primp::Impersonate::ChromeV146,
        "chrome" => primp::Impersonate::Chrome,
        "edge_v144" => primp::Impersonate::EdgeV144,
        "edge_v145" => primp::Impersonate::EdgeV145,
        "edge_v146" => primp::Impersonate::EdgeV146,
        "edge" => primp::Impersonate::Edge,
        "opera_v126" => primp::Impersonate::OperaV126,
        "opera_v127" => primp::Impersonate::OperaV127,
        "opera_v128" => primp::Impersonate::OperaV128,
        "opera_v129" => primp::Impersonate::OperaV129,
        "opera" => primp::Impersonate::Opera,
        "safari_v18_5" => primp::Impersonate::SafariV18_5,
        "safari_v26" => primp::Impersonate::SafariV26,
        "safari_v26_3" => primp::Impersonate::SafariV26_3,
        "safari" => primp::Impersonate::Safari,
        "firefox_v140" => primp::Impersonate::FirefoxV140,
        "firefox_v146" => primp::Impersonate::FirefoxV146,
        "firefox_v147" => primp::Impersonate::FirefoxV147,
        "firefox_v148" => primp::Impersonate::FirefoxV148,
        "firefox" => primp::Impersonate::Firefox,
        "random" => primp::Impersonate::Random,
        _ => return None,
    })
}

fn parse_impersonate_os(name: &str) -> Option<primp::ImpersonateOS> {
    Some(match name {
        "windows" => primp::ImpersonateOS::Windows,
        "macos" => primp::ImpersonateOS::MacOS,
        "linux" => primp::ImpersonateOS::Linux,
        "android" => primp::ImpersonateOS::Android,
        "ios" => primp::ImpersonateOS::IOS,
        "random" => primp::ImpersonateOS::Random,
        _ => return None,
    })
}

// =============================================================================
// HTTP Requests
// =============================================================================

/// Perform an HTTP request. Blocks until complete.
///
/// - `method`: HTTP method as UTF-8 C string ("GET", "POST", etc.)
/// - `url`: Request URL as UTF-8 C string
/// - `body_ptr`/`body_len`: Optional request body (null/0 for no body)
/// - `headers_json`: Optional extra headers as JSON object string (null for none)
/// - `out_response`: Output pointer for the response handle
///
/// Returns 0 on success, negative on error.
#[no_mangle]
pub extern "C" fn primp_request(
    client: *const FfiClient,
    method: *const c_char,
    url: *const c_char,
    body_ptr: *const u8,
    body_len: usize,
    headers_json: *const c_char,
    out_response: *mut *mut FfiResponse,
) -> i32 {
    if client.is_null() || method.is_null() || url.is_null() || out_response.is_null() {
        set_last_error("null pointer argument");
        return -1;
    }

    let client = unsafe { &(*client).inner };
    let method_str = match unsafe { CStr::from_ptr(method) }.to_str() {
        Ok(s) => s,
        Err(_) => {
            set_last_error("invalid UTF-8 in method");
            return -1;
        }
    };
    let url_str = match unsafe { CStr::from_ptr(url) }.to_str() {
        Ok(s) => s,
        Err(_) => {
            set_last_error("invalid UTF-8 in URL");
            return -1;
        }
    };

    let http_method = match method_str.to_uppercase().as_str() {
        "GET" => primp::Method::GET,
        "POST" => primp::Method::POST,
        "PUT" => primp::Method::PUT,
        "PATCH" => primp::Method::PATCH,
        "DELETE" => primp::Method::DELETE,
        "HEAD" => primp::Method::HEAD,
        "OPTIONS" => primp::Method::OPTIONS,
        _ => {
            set_last_error(&format!("unsupported HTTP method: {method_str}"));
            return -2;
        }
    };

    let mut request = client.request(http_method, url_str);

    // Body
    if !body_ptr.is_null() && body_len > 0 {
        let body = unsafe { std::slice::from_raw_parts(body_ptr, body_len) }.to_vec();
        request = request.body(body);
    }

    // Extra headers
    if !headers_json.is_null() {
        let json_str = match unsafe { CStr::from_ptr(headers_json) }.to_str() {
            Ok(s) => s,
            Err(_) => {
                set_last_error("invalid UTF-8 in headers JSON");
                return -1;
            }
        };
        let map: std::collections::HashMap<String, String> = match serde_json::from_str(json_str) {
            Ok(m) => m,
            Err(e) => {
                set_last_error(&format!("invalid headers JSON: {e}"));
                return -4;
            }
        };
        for (k, v) in &map {
            request = request.header(k.as_str(), v.as_str());
        }
    }

    // Execute
    let result = rt().block_on(async {
        let resp = request.send().await?;
        let status = resp.status().as_u16();
        let url = resp.url().to_string();

        // Collect headers
        let headers: std::collections::HashMap<String, String> = resp
            .headers()
            .iter()
            .map(|(k, v)| (k.to_string(), v.to_str().unwrap_or("").to_string()))
            .collect();
        let headers_json = serde_json::to_string(&headers).unwrap_or_default();

        let body = resp.bytes().await?.to_vec();

        Ok::<_, primp::Error>((status, headers_json, body, url))
    });

    match result {
        Ok((status, headers_json, body, url)) => {
            let resp = Box::new(FfiResponse {
                status,
                headers_json: CString::new(headers_json).unwrap_or_default(),
                body,
                url: CString::new(url).unwrap_or_default(),
            });
            unsafe { *out_response = Box::into_raw(resp) };
            0
        }
        Err(e) => {
            set_last_error(&format!("request failed: {e}"));
            -10
        }
    }
}

// =============================================================================
// Response accessors
// =============================================================================

#[no_mangle]
pub extern "C" fn primp_response_status(response: *const FfiResponse) -> u16 {
    if response.is_null() {
        return 0;
    }
    unsafe { (*response).status }
}

#[no_mangle]
pub extern "C" fn primp_response_body(
    response: *const FfiResponse,
    out_ptr: *mut *const u8,
    out_len: *mut usize,
) {
    if response.is_null() || out_ptr.is_null() || out_len.is_null() {
        return;
    }
    let resp = unsafe { &*response };
    unsafe {
        *out_ptr = resp.body.as_ptr();
        *out_len = resp.body.len();
    }
}

#[no_mangle]
pub extern "C" fn primp_response_headers(response: *const FfiResponse) -> *const c_char {
    if response.is_null() {
        return ptr::null();
    }
    unsafe { (*response).headers_json.as_ptr() }
}

#[no_mangle]
pub extern "C" fn primp_response_url(response: *const FfiResponse) -> *const c_char {
    if response.is_null() {
        return ptr::null();
    }
    unsafe { (*response).url.as_ptr() }
}

// =============================================================================
// Cleanup
// =============================================================================

#[no_mangle]
pub extern "C" fn primp_client_free(client: *mut FfiClient) {
    if !client.is_null() {
        unsafe { drop(Box::from_raw(client)) };
    }
}

#[no_mangle]
pub extern "C" fn primp_response_free(response: *mut FfiResponse) {
    if !response.is_null() {
        unsafe { drop(Box::from_raw(response)) };
    }
}

// =============================================================================
// Version info
// =============================================================================

/// Returns the primp-ffi version as a static C string.
#[no_mangle]
pub extern "C" fn primp_ffi_version() -> *const c_char {
    static VERSION: &[u8] = concat!(env!("CARGO_PKG_VERSION"), "\0").as_bytes();
    VERSION.as_ptr() as *const c_char
}
