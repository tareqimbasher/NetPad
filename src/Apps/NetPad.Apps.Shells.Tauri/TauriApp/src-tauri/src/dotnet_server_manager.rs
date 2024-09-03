use std::borrow::BorrowMut;
use std::process::{Child, Command};
use std::sync::Mutex;
use tauri::path::BaseDirectory;
use tauri::{Manager, State};

pub struct DotNetServerManagerState {
    pub server_manager_mutex: Mutex<DotNetServerManager>,
}

pub struct DotNetServerManager {
    child: Option<Child>,
}

impl DotNetServerManager {
    pub fn new() -> DotNetServerManager {
        DotNetServerManager { child: None }
    }

    pub fn start_backend(&mut self, app_handle: &tauri::AppHandle) -> Result<String, String> {
        let server_path = app_handle
            .path()
            .resolve(
                "resources/netpad-server/NetPad.Apps.App",
                BaseDirectory::Resource,
            )
            .unwrap();

        let working_dir = app_handle
            .path()
            .resolve("resources/netpad-server", BaseDirectory::Resource)
            .unwrap();

        log::info!(
            "Starting backend at path: '{}' with working dir: '{}'",
            server_path.display(),
            working_dir.display()
        );

        let mut cmd = Command::new(server_path);
        cmd.arg("--tauri");
        cmd.current_dir(working_dir);

        match self.child.borrow_mut() {
            Some(c) => {
                let pid = c.id();
                let msg = format!("Requested to start .NET server process but it has already been created. PID: {pid}");
                log::warn!("{msg}");
                Ok(msg)
            }
            None => {
                let child = cmd.spawn();

                match child {
                    Ok(c) => {
                        let pid = c.id();
                        self.child = Some(c);
                        let msg =
                            format!(".NET server process started successfully with PID: {pid}");
                        log::info!("{msg}");
                        Ok(msg)
                    }
                    Err(e) => {
                        let msg = format!(".NET server process failed to start: {e}");
                        log::error!("{msg}");
                        Err(msg)
                    }
                }
            }
        }
    }

    pub fn terminate_backend(&mut self) -> Result<String, String> {
        match self.child.borrow_mut() {
            Some(child) => {
                let pid = child.id().to_string();

                if cfg!(unix) {
                    log::info!("Sending SIGTERM to .NET server process with PID: {pid}");
                    Command::new("kill")
                        .args(["-s", "SIGTERM", &pid])
                        .spawn()
                        .expect("Error stopping .NET server process. Failed to spawn 'kill'")
                        .wait()
                        .expect("Error stopping .NET server process. Failed while waiting for kill to complete");
                } else if cfg!(windows) {
                    log::info!("Using taskkill on .NET server process with PID: {pid}");
                    Command::new("taskkill")
                        .args(["/PID", &pid, "/F"])
                        .spawn()
                        .expect("Error stopping .NET server process. Failed to spawn 'taskkill'")
                        .wait()
                        .expect("Error stopping .NET server process. Failed while waiting for taskkill to complete");
                }

                self.child = None;
                let msg = format!(".NET server process terminated. PID was: {pid}");
                log::info!("{msg}");
                Ok(msg)
            }
            _ => {
                let msg = "Requested to terminate .NET server process but it is not running";
                log::warn!("{msg}");
                Ok(msg.into())
            }
        }
    }

    pub fn restart_backend(&mut self, app_handle: &tauri::AppHandle) -> Result<String, String> {
        log::info!("Restarting .NET server process");
        let terminate_result = self.terminate_backend();
        match terminate_result {
            Ok(_) => {
                self.start_backend(app_handle).unwrap();
                Ok(".NET server was restarted successfully".into())
            }
            Err(e) => {
                log::error!("{e}");
                return Err(e);
            }
        }
    }
}

#[tauri::command]
pub fn start_server(
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
pub fn stop_server(
    server_manager_state: State<DotNetServerManagerState>,
) -> Result<String, String> {
    let am = server_manager_state
        .server_manager_mutex
        .lock()
        .unwrap()
        .terminate_backend();
    am
}

#[tauri::command]
pub fn restart_server(
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
