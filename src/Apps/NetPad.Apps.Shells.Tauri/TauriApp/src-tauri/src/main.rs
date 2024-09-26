// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    // Setting this env var resolves an issue on some linux setups with NVIDIA graphics
    // where the app launches with an empty screen and the error:
    //      Failed to create GBM buffer of size {win_width}x{win_height}: Invalid argument
    //
    // TODO only disable on systems with NVIDIA graphics
    #[cfg(any(
        target_os = "linux",
        target_os = "freebsd",
        target_os = "dragonfly",
        target_os = "openbsd",
        target_os = "netbsd"
    ))]
    std::env::set_var("WEBKIT_DISABLE_DMABUF_RENDERER", "1");

    app_lib::run();
}
