use std::{
    collections::HashMap,
    path::PathBuf,
    sync::{Arc, Mutex},
};
use tauri::{
    webview::DownloadEvent, AppHandle, Emitter, Manager, WebviewUrl, WebviewWindow,
    WebviewWindowBuilder,
};
use tauri_plugin_opener::OpenerExt;
use tauri_plugin_window_state::AppHandleExt;

use crate::errors::Result;

pub const MAIN_WINDOW_LABEL: &str = "main";

pub struct WindowCreationOptions {
    pub parent: Option<WebviewWindow>,
    pub label: String,
    pub title: String,
    pub url: WebviewUrl,
    pub width: f64,
    pub height: f64,
    pub maximize: bool,
    pub visible: bool,
    pub position: Option<(f64, f64)>,
    pub center: bool,
    pub decorations: bool,
    pub disable_drag_drop: bool,
}

pub fn create_main_window(app_handle: &AppHandle) -> Result<WebviewWindow> {
    let window = create_window(
        app_handle,
        WindowCreationOptions {
            parent: None,
            label: MAIN_WINDOW_LABEL.into(),
            title: "NetPad".into(),
            url: WebviewUrl::App("index.html".into()),
            width: 1200f64,
            height: 800f64,
            maximize: !has_saved_window_state(app_handle, MAIN_WINDOW_LABEL),
            visible: false,
            position: None,
            center: false,
            decorations: false,
            disable_drag_drop: false,
        },
    )?;

    window.show()?;
    window.set_focus()?;

    Ok(window)
}

pub fn create_window(
    app_handle: &AppHandle,
    options: WindowCreationOptions,
) -> Result<WebviewWindow> {
    let download_memory = Arc::new(Mutex::new(DownloadShortTermMemory::default()));

    let mut builder = WebviewWindowBuilder::new(app_handle, options.label, options.url)
        .title(options.title)
        .inner_size(options.width, options.height)
        .maximized(options.maximize)
        .visible(options.visible)
        .decorations(options.decorations)
        .on_download({
            let app_handle = app_handle.clone();
            move |_webview, event| {
                match event {
                    DownloadEvent::Requested { url, destination } => {
                        let _ = url; // to appease clippy

                        let mut abs_path = app_handle.path().download_dir().unwrap();
                        abs_path.push(destination.clone());

                        let mut memory = download_memory.lock().unwrap();
                        memory.file_path = Some(abs_path.clone());

                        *destination = abs_path;
                    }
                    DownloadEvent::Finished { url, path, success } => {
                        let _ = url; // to appease clippy
                        let _ = path;

                        if success {
                            let memory = download_memory.lock().unwrap();

                            let path_str = memory
                                .file_path
                                .as_ref()
                                .and_then(|p| {
                                    dunce::canonicalize(p)
                                        .ok()
                                        .and_then(|p| p.into_os_string().into_string().ok())
                                })
                                .unwrap_or_default();

                            app_handle
                                .emit_to(MAIN_WINDOW_LABEL, "download-finished", &path_str)
                                .ok();

                            if !path_str.is_empty() {
                                app_handle.opener().open_path(path_str, None::<&str>).ok();
                            }
                        }
                    }
                    _ => (),
                }
                // let the download start
                true
            }
        })
        .on_navigation({
            let app_handle = app_handle.clone();
            move |url| {
                // Allow loading HTML bundled with Tauri app (loader/index.html)
                // Linux/macOS scheme: tauri
                // Windows host: tauri.localhost
                if url.scheme() == "tauri" || url.host_str() == Some("tauri.localhost") {
                    return true;
                }

                // Allow when loader/index.html reroutes to SPA app hosted by .NET server
                if url.host_str() == Some("localhost") {
                    return url.port() == Some(57950);
                }

                // Reroute other URLs to system browser
                app_handle
                    .opener()
                    .open_url(url.to_string(), None::<&str>)
                    .ok();
                false
            }
        });

    if let Some(parent) = options.parent {
        builder = builder.parent(&parent)?;
    }

    if let Some(position) = options.position {
        builder = builder.position(position.0, position.1);
    }

    if options.disable_drag_drop {
        builder = builder.disable_drag_drop_handler();
    }

    let window = builder.build()?;

    if options.center {
        window.center()?;
    }

    Ok(window)
}

/// Whether the window-state plugin has saved a state for the window with this label.
///
/// The plugin keeps its loaded state private, so this reads the file it saves to. It
/// treats a file that cannot be read or parsed as having no saved state.
fn has_saved_window_state(app_handle: &AppHandle, label: &str) -> bool {
    app_handle
        .path()
        .app_config_dir()
        .ok()
        .and_then(|dir| std::fs::read_to_string(dir.join(app_handle.filename())).ok())
        .is_some_and(|contents| state_file_has_window(&contents, label))
}

fn state_file_has_window(state_file_contents: &str, label: &str) -> bool {
    serde_json::from_str::<HashMap<String, serde_json::Value>>(state_file_contents)
        .is_ok_and(|states| states.contains_key(label))
}

/// Used to store the path to the last downloaded file
///
/// Needed because WebviewWindowBuilder.on_download hook's Finished event does not always
/// return the path the file was downloaded to.
#[derive(Default)]
struct DownloadShortTermMemory {
    file_path: Option<PathBuf>,
}

#[cfg(test)]
mod tests {
    use super::state_file_has_window;

    #[test]
    fn state_file_with_an_entry_for_the_label_has_the_window() {
        let contents = r#"{
            "main": {
                "width": 1380, "height": 523, "x": 0, "y": 0, "prev_x": 0, "prev_y": 0,
                "maximized": false, "visible": true, "decorated": false, "fullscreen": false
            }
        }"#;

        assert!(state_file_has_window(contents, "main"));
    }

    #[test]
    fn state_file_without_an_entry_for_the_label_does_not_have_the_window() {
        let contents = r#"{ "other": { "width": 800, "height": 600 } }"#;

        assert!(!state_file_has_window(contents, "main"));
    }

    #[test]
    fn empty_state_file_does_not_have_the_window() {
        assert!(!state_file_has_window("", "main"));
    }

    #[test]
    fn invalid_state_file_does_not_have_the_window() {
        assert!(!state_file_has_window(r#"{ "main": "#, "main"));
    }

    #[test]
    fn state_file_that_is_not_an_object_does_not_have_the_window() {
        assert!(!state_file_has_window(r#"["main"]"#, "main"));
    }
}
