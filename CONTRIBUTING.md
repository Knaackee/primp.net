# Contributing

Thanks for contributing to primp.net.

## Requirements

- .NET 10 SDK (recommended) and .NET 8 SDK.
- Rust 1.84+ toolchain (for building the native FFI crate).
- Run all tests before opening a pull request.

## Workflow

1. Fork and create a topic branch.
2. Implement a focused change.
3. Add or update tests.
4. Open a PR with a clear summary and validation steps.

## Native Library

The native library (`primp_ffi`) is built from Rust and wraps the original
[primp](https://github.com/deedy5/primp) crate. Changes to `native/primp-ffi/`
require a Rust toolchain to rebuild.
