// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    #[cfg(target_os = "linux")]
    linux::nvidia_graphics_fix();
    app_lib::run().expect("Error running application");
}

#[cfg(target_os = "linux")]
mod linux {
    use std::fs;
    use std::path::Path;

    pub fn nvidia_graphics_fix() {
        if nvidia_detected() {
            // Setting this env var resolves an issue on some linux setups with NVIDIA graphics
            // drivers where the app launches with an empty screen and the error:
            //      Failed to create GBM buffer of size {win_width}x{win_height}: Invalid argument
            std::env::set_var("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
        }
    }

    fn nvidia_detected() -> bool {
        let drm_path = Path::new("/sys/class/drm");
        if let Ok(entries) = fs::read_dir(drm_path) {
            for entry in entries.flatten() {
                let vendor_file = entry.path().join("device/vendor");
                if let Ok(contents) = fs::read_to_string(&vendor_file) {
                    if contents.trim() == "0x10de" {
                        return true;
                    }
                }
            }
        }
        false
    }
}
