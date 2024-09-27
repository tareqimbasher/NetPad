use dotnet_server_manager::{
    restart_server, start_server, stop_server, DotNetServerManager, DotNetServerManagerState,
};
use std::path::PathBuf;
use std::sync::{Arc, Mutex};
use tauri::webview::DownloadEvent;
use tauri::{
    AppHandle, Emitter, Error, Manager, State, Url, WebviewUrl, WebviewWindow,
    WebviewWindowBuilder, WindowEvent,
};
use tauri_plugin_log::{Target, TargetKind};
use tauri_plugin_shell::ShellExt;

pub mod dotnet_server_manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let server_manager = DotNetServerManager::new();
    let server_manager_state = DotNetServerManagerState {
        server_manager_mutex: Mutex::new(server_manager),
    };

    tauri::Builder::default()
        .plugin(
            tauri_plugin_log::Builder::new()
                .clear_targets()
                .target(Target::new(TargetKind::Stdout))
                .build(),
        )
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_shell::init())
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
            get_os_type,
            create_window_from_js,
            toggle_devtools,
            start_server,
            stop_server,
            restart_server,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[tauri::command]
fn get_os_type() -> String {
    std::env::consts::OS.to_string()
}

fn create_window(app_handle: &AppHandle, options: WindowCreationOptions) -> Result<WebviewWindow, Error> {
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
                        let mut abs_path = app_handle.path().download_dir().unwrap();
                        abs_path.push(&destination);

                        let mut memory = download_memory.lock().unwrap();
                        memory.file_path = Some(abs_path.clone());

                        *destination = abs_path;
                    }
                    DownloadEvent::Finished { url, path, success } => {
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
                                app_handle.shell().open(path_str, None).ok();
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
                // Reroute non app URLs to system browser
                if url.scheme() == "tauri" {
                    return true;
                }

                if url.host_str() == Some("localhost") {
                    return if cfg!(dev) {
                        url.port() == Some(57940)
                    } else {
                        url.port() == Some(57930)
                    };
                }

                app_handle.shell().open(url.to_string(), None).ok();

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

/// Command to open a window from JavaScript
#[allow(clippy::too_many_arguments)]
#[tauri::command(async)]
async fn create_window_from_js(
    app_handle: AppHandle,
    calling_window: WebviewWindow,
    label: String,
    title: String,
    url: WebviewUrl,
    width: f64,
    height: f64,
    x: f64,
    y: f64,
) -> Result<(), Error> {
    create_window(
        &app_handle,
        WindowCreationOptions {
            parent: Some(calling_window),
            label,
            title,
            url,
            width,
            height,
            maximize: false,
            position: Some((x, y)),
            center: true,
            decorations: true,
            disable_drag_drop: false,
        },
    )?;
    Ok(())
}

#[tauri::command]
async fn toggle_devtools(webview_window: WebviewWindow) {
    if webview_window.is_devtools_open() {
        webview_window.close_devtools();
    } else {
        webview_window.open_devtools();
    }
}

struct WindowCreationOptions {
    parent: Option<WebviewWindow>,
    label: String,
    title: String,
    url: WebviewUrl,
    width: f64,
    height: f64,
    maximize: bool,
    position: Option<(f64, f64)>,
    center: bool,
    decorations: bool,
    disable_drag_drop: bool,
}

/// Used to store the path to the last downloaded file
///
/// Needed because WebviewWindowBuilder.on_download hook's Finished event does not always
/// return the path the file was downloaded to.
#[derive(Default)]
struct DownloadShortTermMemory {
    file_path: Option<PathBuf>,
}
