mod commands;
mod dotnet_server_manager;
mod errors;

use std::path::PathBuf;
use std::sync::{Arc, Mutex};

use tauri::{
    webview::DownloadEvent, AppHandle, Emitter, Manager, State, WebviewUrl, WebviewWindow,
    WebviewWindowBuilder, WindowEvent,
};
use tauri_plugin_log::{Target, TargetKind};
use tauri_plugin_opener::OpenerExt;

use crate::commands::{create_window_command, get_os_type, toggle_devtools};
use crate::dotnet_server_manager::{DotNetServerManager, DotNetServerManagerState};
use crate::errors::Result;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() -> Result<()> {
    errors::init()?;

    let server_manager_state = DotNetServerManagerState {
        server_manager_mutex: Mutex::new(DotNetServerManager::default()),
    };

    tauri::Builder::default()
        .plugin(
            tauri_plugin_log::Builder::new()
                .clear_targets()
                .target(Target::new(TargetKind::Stdout))
                .build(),
        )
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_opener::init())
        .manage(server_manager_state)
        .setup(move |app| {
            if cfg!(not(debug_assertions)) {
                let state: State<DotNetServerManagerState> = app.state();
                state
                    .server_manager_mutex
                    .lock()
                    .unwrap()
                    .start_backend(app.handle())
                    .expect("Failed to start .NET server");
            }

            create_window(
                app.handle(),
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
            )?;

            Ok(())
        })
        .on_window_event(move |window, event| {
            if let WindowEvent::Destroyed = event {
                if cfg!(not(debug_assertions)) && window.label() == "main" {
                    let state: State<DotNetServerManagerState> = window.state();
                    state
                        .server_manager_mutex
                        .lock()
                        .unwrap()
                        .terminate_backend()
                        .expect("Failed to terminate .NET server on 'main' window destroyed event");
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            create_window_command,
            get_os_type,
            toggle_devtools,
        ])
        .run(tauri::generate_context!())?;

    Ok(())
}

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
                    return url.port() == Some(57950)
                }

                // Reroute other URLs to system browser
                app_handle.opener().open_url(url.to_string(), None::<&str>).ok();
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
