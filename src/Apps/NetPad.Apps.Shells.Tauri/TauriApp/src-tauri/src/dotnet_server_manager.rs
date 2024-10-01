use std::borrow::BorrowMut;
#[cfg(target_os = "windows")]
use std::os::windows::process::CommandExt;
use std::process::{Child, Command};
use std::sync::Mutex;

use tauri::path::BaseDirectory;
use tauri::Manager;

pub struct DotNetServerManagerState {
    pub server_manager_mutex: Mutex<DotNetServerManager>,
}

#[derive(Default)]
pub struct DotNetServerManager {
    child: Option<Child>,
}

impl DotNetServerManager {
    pub fn start_backend(&mut self, app_handle: &tauri::AppHandle) -> Result<(), String> {
        let exe_ext = if std::env::consts::OS == "windows" {
            ".exe"
        } else {
            ""
        };

        let mut executable_path = app_handle
            .path()
            .resolve(
                format!("resources/netpad-server/NetPad.Apps.App{exe_ext}"),
                BaseDirectory::Resource,
            )
            .unwrap();

        if !executable_path.exists() {
            // If running standalone app and resources folder is in same dir as executable
            if let Ok(current_exe) = std::env::current_exe() {
                executable_path = current_exe;
                executable_path.pop();
                executable_path.push("resources");
                executable_path.push("netpad-server");
                executable_path.push(format!("NetPad.Apps.App{exe_ext}"));
            }

            if !executable_path.exists() {
                let msg = format!(
                    ".NET server executable was not found at path: '{}'",
                    executable_path.display()
                );
                log::error!("{msg}");
                return Err(msg);
            }
        }

        let mut working_dir = executable_path.clone();
        working_dir.pop();

        log::info!(
            "Starting .NET server backend at path: '{}' with working dir: '{}'",
            executable_path.display(),
            working_dir.display()
        );

        let mut cmd = Command::new(executable_path);
        cmd.arg("--tauri");
        cmd.arg("--parent-pid");
        cmd.arg(std::process::id().to_string());
        cmd.current_dir(dunce::canonicalize(working_dir).unwrap());
        #[cfg(target_os = "windows")]
        {
            const CREATE_NO_WINDOW: u32 = 0x08000000;
            cmd.creation_flags(CREATE_NO_WINDOW);
        }

        match self.child.borrow_mut() {
            Some(c) => {
                log::warn!("Requested to start .NET server process but it has already been created. PID: {}", c.id());
                Ok(())
            }
            None => match cmd.spawn() {
                Ok(c) => {
                    let pid = c.id();
                    self.child = Some(c);
                    log::info!(".NET server process started successfully with PID: {}", pid);
                    Ok(())
                }
                Err(e) => {
                    let msg = format!(".NET server process failed to start: {e}");
                    log::error!("{msg}");
                    Err(msg)
                }
            },
        }
    }

    pub fn terminate_backend(&mut self) -> Result<(), String> {
        match self.child.borrow_mut() {
            Some(child) => {
                let pid = child.id().to_string();

                #[cfg(unix)]
                {
                    log::info!("Sending SIGTERM to .NET server process with PID: {pid}");
                    Command::new("kill")
                        .args(["-s", "SIGTERM", &pid])
                        .spawn()
                        .expect("Error stopping .NET server process. Failed to spawn 'kill'")
                        .wait()
                        .expect("Error stopping .NET server process. Failed while waiting for kill to complete");
                }

                #[cfg(windows)]
                {
                    log::info!("Using taskkill on .NET server process with PID: {pid}");
                    const CREATE_NO_WINDOW: u32 = 0x08000000;
                    Command::new("taskkill")
                        .args(["/PID", &pid, "/F"])
                        .creation_flags(CREATE_NO_WINDOW)
                        .spawn()
                        .expect("Error stopping .NET server process. Failed to spawn 'taskkill'")
                        .wait()
                        .expect("Error stopping .NET server process. Failed while waiting for taskkill to complete");
                }

                self.child = None;
                log::info!(".NET server process terminated. PID was: {pid}");
            }
            _ => {
                log::warn!("Requested to terminate .NET server process but it is not running");
            }
        }
        Ok(())
    }
}
