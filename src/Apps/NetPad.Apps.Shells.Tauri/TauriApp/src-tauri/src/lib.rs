use dotnet_server_manager::{
    restart_server, start_server, stop_server, DotNetServerManager, DotNetServerManagerState,
};
use std::sync::Mutex;
use tauri::{
    AppHandle, Error, Manager, State, Url, WebviewUrl, WebviewWindow, WebviewWindowBuilder,
    WindowEvent,
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

            open_window(
                app.handle(),
                None,
                "main".into(),
                "NetPad".into(),
                WebviewUrl::App("index.html".into()),
                1200f64,
                800f64,
                true,
                None,
                false,
                false,
                true,
            )?;

            Ok(())
        })
        .on_window_event(move |window, event| match event {
            WindowEvent::Destroyed => {
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
            _ => {}
        })
        .invoke_handler(tauri::generate_handler![
            get_os_type,
            open_window_cmd,
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

fn open_window(
    app_handle: &AppHandle,
    parent: Option<WebviewWindow>,
    label: String,
    title: String,
    url: WebviewUrl,
    width: f64,
    height: f64,
    maximized: bool,
    position: Option<(f64, f64)>,
    center: bool,
    decorations: bool,
    disable_drag_drop: bool,
) -> Result<(), Error> {
    let app_handle_clone = app_handle.clone();

    let mut builder = WebviewWindowBuilder::new(app_handle, label, url)
        .title(title)
        .inner_size(width, height)
        .maximized(maximized)
        .decorations(decorations)
        .on_navigation(move |url| {
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

            app_handle_clone.shell().open(url.to_string(), None).ok();

            return false;
        });

    if parent.is_some() {
        builder = builder.parent(&parent.unwrap())?;
    }

    if let Some(pos) = position {
        builder = builder.position(pos.0, pos.1);
    }

    if disable_drag_drop {
        builder = builder.disable_drag_drop_handler();
    }

    let window = builder.build()?;

    if center {
        window.center()?;
    }

    Ok(())
}

/// Command to open a window from JavaScript
#[tauri::command(async)]
async fn open_window_cmd(
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
    open_window(
        &app_handle,
        Some(calling_window),
        label,
        title,
        url,
        width,
        height,
        false,
        Some((x, y)),
        true,
        true,
        false,
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
