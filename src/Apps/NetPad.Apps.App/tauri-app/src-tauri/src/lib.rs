pub mod server_manager;
use server_manager::ServerManager;
use std::sync::Mutex;
use tauri::{Manager, State, WindowEvent};

struct ServerManagerState {
    server_manager_mutex: Mutex<ServerManager>,
}

#[tauri::command]
fn start_server(
    server_manager_state: State<ServerManagerState>,
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
fn stop_server(server_manager_state: State<ServerManagerState>) -> Result<String, String> {
    let am = server_manager_state
        .server_manager_mutex
        .lock()
        .unwrap()
        .terminate_backend();
    am
}

#[tauri::command]
fn restart_server(
    server_manager_state: State<ServerManagerState>,
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
    let server_manager = ServerManager::new();
    let sms = ServerManagerState {
        server_manager_mutex: Mutex::new(server_manager),
    };

    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(sms)
        .setup(move |app| {
            if cfg!(not(debug_assertions)) {
                let state: State<ServerManagerState> = app.state();
                state
                    .server_manager_mutex
                    .lock()
                    .unwrap()
                    .start_backend(app.handle())
                    .expect("Backend start failed");
            }

            // let window = app.get_webview_window("main").unwrap();
            // window.open_devtools();
            // window.close_devtools();

            Ok(())
        })
        .on_window_event(move |window, event| match event {
            WindowEvent::Destroyed => {
                if cfg!(not(debug_assertions)) && window.label() == "main" {
                    let state: State<ServerManagerState> = window.state();
                    state
                        .server_manager_mutex
                        .lock()
                        .unwrap()
                        .terminate_backend()
                        .expect("Failed to terminate backend on window destroyed event");
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
