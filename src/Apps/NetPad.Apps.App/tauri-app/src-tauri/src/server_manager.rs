use std::borrow::BorrowMut;
use std::process::{Child, Command};
use tauri::path::BaseDirectory;
use tauri::Manager;

pub struct ServerManager {
    child: Option<Child>,
}

impl ServerManager {
    pub fn new() -> ServerManager {
        ServerManager { child: None }
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

        let mut cmd = Command::new(server_path);
        cmd.current_dir(working_dir);

        match self.child.borrow_mut() {
            Some(_) => {
                let info = "Server process is already created";
                println!("{}", &info);
                Ok(info.into())
            }
            None => {
                let child = cmd.spawn();
                match child {
                    Ok(v) => {
                        self.child = Some(v);
                        let info = "Server started successfully";
                        println!("{}", &info);
                        Ok(info.into())
                    }
                    Err(_) => {
                        let info = "Server failed to start";
                        println!("{}", &info);
                        Err(info.into())
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
                    println!("Sending SIGTERM to process with PID: {}", pid);
                    Command::new("kill")
                        .args(["-s", "SIGTERM", &pid])
                        .spawn()
                        .expect("Failed to spawn 'kill'")
                        .wait()
                        .expect("Failed while waiting for kill to complete");
                }

                if cfg!(windows) {
                    println!("Using taskkill on process with PID: {}", pid);
                    Command::new("taskkill")
                        .args(["/PID", &pid, "/F"])
                        .spawn()
                        .expect("Failed to spawn 'taskkill'")
                        .wait()
                        .expect("Failed while waiting for taskkill to complete");
                }

                self.child = None;
                let info = "Server terminated";
                println!("{}", &info);
                Ok(info.into())
            }
            _ => {
                let info = "Server process is not running";
                println!("{}", &info);
                Ok(info.into())
            }
        }
    }

    pub fn restart_backend(&mut self, app_handle: &tauri::AppHandle) -> Result<String, String> {
        println!("Restarting server process");
        let terminate_result = self.terminate_backend();
        match terminate_result {
            Ok(_) => match self.start_backend(app_handle) {
                Ok(_) => {
                    let info = "Server restarted successfully";
                    println!("{}", &info);
                    Ok(info.into())
                }
                Err(e) => {
                    println!("{}", &e);
                    return Err(e.into());
                }
            },
            Err(e) => {
                println!("{}", &e);
                return Err(e);
            }
        }
    }
}
