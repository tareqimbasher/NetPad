use dotnet_server_manager::DotNetServerManager;
use std::sync::Mutex;
use tauri::{Manager, State, WindowEvent};
use tauri_plugin_log::{Target, TargetKind};

pub mod dotnet_server_manager;

struct DotNetServerManagerState {
    server_manager_mutex: Mutex<DotNetServerManager>,
}

#[tauri::command]
fn start_server(
    server_manager_state: State<DotNetServerManagerState>,
    app_handle: tauri::AppHandle,
) -> Result<String, String> {
    let am = server_manager_state
        .server_manager_mutex
        .lock()
        .unwrap()
        .start_backend(&app_handle);
    am
}

#[tauri::command]
fn stop_server(server_manager_state: State<DotNetServerManagerState>) -> Result<String, String> {
    let am = server_manager_state
        .server_manager_mutex
        .lock()
        .unwrap()
        .terminate_backend();
    am
}

#[tauri::command]
fn restart_server(
    server_manager_state: State<DotNetServerManagerState>,
    app_handle: tauri::AppHandle,
) -> Result<String, String> {
    let am = server_manager_state
        .server_manager_mutex
        .lock()
        .unwrap()
        .restart_backend(&app_handle);
    am
}

#[tauri::command]
fn toggle_devtools(webview_window: tauri::WebviewWindow) {
    if webview_window.is_devtools_open() {
        webview_window.close_devtools();
    } else {
        webview_window.open_devtools();
    }
}

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
            start_server,
            stop_server,
            restart_server,
            toggle_devtools
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}