use tauri::{AppHandle, WebviewUrl, WebviewWindow};

use crate::errors::Result;
use crate::{create_window, WindowCreationOptions};

#[tauri::command]
pub fn get_os_type() -> String {
    std::env::consts::OS.to_string()
}

#[tauri::command(async)]
pub async fn toggle_devtools(webview_window: WebviewWindow) {
    if webview_window.is_devtools_open() {
        webview_window.close_devtools();
    } else {
        webview_window.open_devtools();
    }
}

#[allow(clippy::too_many_arguments)]
#[tauri::command(async)]
pub async fn create_window_command(
    app_handle: AppHandle,
    calling_window: WebviewWindow,
    label: String,
    title: String,
    url: WebviewUrl,
    width: f64,
    height: f64,
    x: f64,
    y: f64,
) -> Result<()> {
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
