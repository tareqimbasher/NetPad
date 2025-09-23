mod commands;
mod dotnet_server_manager;
mod errors;
mod windows;

use std::sync::Mutex;

use tauri::{Manager, State, WindowEvent};
use tauri_plugin_log::{Target, TargetKind};

use crate::commands::{create_window_command, get_os_type, toggle_devtools};
use crate::dotnet_server_manager::{DotNetServerManager, DotNetServerManagerState};
use crate::errors::Result;
use crate::windows::{create_main_window, create_window, WindowCreationOptions};

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
            // Start .NET app and open main window
            if cfg!(not(debug_assertions)) {
                let state: State<DotNetServerManagerState> = app.state();
                state
                    .server_manager_mutex
                    .lock()
                    .unwrap()
                    .start_backend(app.handle())
                    .expect("Failed to start .NET server");
            }

            create_main_window(app.handle())?;

            Ok(())
        })
        .on_window_event(move |window, event| {
            // Stop .NET app when main window is closed
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
