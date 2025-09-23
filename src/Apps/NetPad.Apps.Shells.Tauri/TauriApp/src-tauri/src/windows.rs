use std::{
    path::PathBuf,
    sync::{Arc, Mutex},
};
use tauri::{
    webview::DownloadEvent, AppHandle, Emitter, Manager, WebviewUrl, WebviewWindow,
    WebviewWindowBuilder,
};
use tauri_plugin_opener::OpenerExt;

use crate::errors::Result;

pub struct WindowCreationOptions {
    pub parent: Option<WebviewWindow>,
    pub label: String,
    pub title: String,
    pub url: WebviewUrl,
    pub width: f64,
    pub height: f64,
    pub maximize: bool,
    pub position: Option<(f64, f64)>,
    pub center: bool,
    pub decorations: bool,
    pub disable_drag_drop: bool,
}

pub fn create_main_window(app_handle: &AppHandle) -> Result<WebviewWindow> {
    create_window(
        app_handle,
        WindowCreationOptions {
            parent: None,
            label: "main".into(),
            title: "NetPad".into(),
            url: WebviewUrl::App("index.html".into()),
            width: 1200f64,
            height: 800f64,
            maximize: true,
            position: None,
            center: false,
            decorations: false,
            disable_drag_drop: true,
        },
    )
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
                                .emit_to("main", "download-finished", &path_str)
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

/// Used to store the path to the last downloaded file
///
/// Needed because WebviewWindowBuilder.on_download hook's Finished event does not always
/// return the path the file was downloaded to.
#[derive(Default)]
struct DownloadShortTermMemory {
    file_path: Option<PathBuf>,
}
