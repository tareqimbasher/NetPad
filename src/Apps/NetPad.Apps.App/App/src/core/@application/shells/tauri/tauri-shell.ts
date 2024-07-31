import {IAurelia, Registration} from "aurelia";
import {IShell} from "../ishell";
import {IBackgroundService, IIpcGateway, IWindowService} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {TauriWindowBackgroundService} from "@application/shells/tauri/services/tauri-window-background-service";
import {TauriDialogBackgroundService} from "@application/shells/tauri/services/tauri-dialog-background-service";
import {TauriWindowService} from "@application/shells/tauri/services/tauri-window-service";

export class TauriShell implements IShell {
    public configure(appBuilder: IAurelia): void {
        appBuilder.register(
            Registration.transient(IBackgroundService, TauriDialogBackgroundService),
            Registration.transient(IBackgroundService, TauriWindowBackgroundService),
            Registration.transient(IWindowService, TauriWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
        );

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
