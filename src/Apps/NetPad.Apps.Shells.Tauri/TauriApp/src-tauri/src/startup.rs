use tauri::Manager;

use crate::dotnet_server_manager::{
    is_server_ready, read_connection_file, DotNetServerManagerState,
};

/// Starts the .NET backend, then polls for its connection file and navigates the
/// main window to the SPA once the server is ready.
pub fn start_backend_and_navigate(app: &mut tauri::App) {
    let state: tauri::State<DotNetServerManagerState> = app.state();
    let backend_pid = state
        .server_manager_mutex
        .lock()
        .unwrap()
        .start_backend(app.handle())
        .expect("Failed to start .NET server");

    let app_handle = app.handle().clone();
    std::thread::spawn(move || {
        let timeout = std::time::Duration::from_secs(30);
        let poll_interval = std::time::Duration::from_millis(250);
        let start = std::time::Instant::now();

        let info = loop {
            if start.elapsed() > timeout {
                log::error!("Timed out waiting for connection file");
                show_timeout_error(&app_handle);
                return;
            }

            if let Some(info) = read_connection_file(backend_pid) {
                break info;
            }

            std::thread::sleep(poll_interval);
        };

        if let Ok(parsed_url) = url::Url::parse(&info.url) {
            let port = parsed_url.port().unwrap_or(80);
            while !is_server_ready(port) && start.elapsed() <= timeout {
                std::thread::sleep(poll_interval);
            }
        }

        if let Some(window) = app_handle.get_webview_window("main") {
            let nav_url = format!("{}?shell=tauri&token={}", info.url, info.token);
            match url::Url::parse(&nav_url) {
                Ok(url) => {
                    log::info!("Navigating main window to NetPad SPA");
                    if let Err(e) = window.navigate(url) {
                        log::error!("Failed to navigate main window: {e}");
                    }
                }
                Err(e) => log::error!("Failed to parse navigation URL: {e}"),
            }
        }
    });
}

fn show_timeout_error(app_handle: &tauri::AppHandle) {
    if let Some(window) = app_handle.get_webview_window("main") {
        window
            .eval(
                "document.getElementById('loader').remove(); \
                 document.getElementById('error').style.display = 'block'; \
                 document.getElementById('error-details').innerHTML = \
                 'NetPad backend did not start within 30 seconds.';",
            )
            .ok();
    }
}
