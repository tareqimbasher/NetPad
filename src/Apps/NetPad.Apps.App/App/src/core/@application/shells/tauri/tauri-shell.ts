import {IAurelia, Registration} from "aurelia";
import {IShell} from "../ishell";
import {IBackgroundService, IIpcGateway, IWindowService, Settings} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {TauriWindowBackgroundService} from "@application/shells/tauri/services/tauri-window-background-service";
import {TauriDialogBackgroundService} from "@application/shells/tauri/services/tauri-dialog-background-service";
import {TauriWindowService} from "@application/shells/tauri/services/tauri-window-service";
import {NativeMainMenuEventHandler} from "./services/native-main-menu-event-handler";
import {Window} from "@tauri-apps/api/window"
import {WindowId} from "@application/windows/window-id";
import {WindowParams} from "@application/windows/window-params";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {TauriNativeDialogService} from "@application/shells/tauri/services/tauri-native-dialog-service";

export class TauriShell implements IShell {
    public configure(appBuilder: IAurelia): void {
        appBuilder.register(
            Registration.transient(IBackgroundService, TauriDialogBackgroundService),
            Registration.transient(IBackgroundService, TauriWindowBackgroundService),
            Registration.transient(IWindowService, TauriWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
            Registration.singleton(INativeDialogService, TauriNativeDialogService),
        );

        if (WindowParams.window === WindowId.Main) {
            const settings = appBuilder.container.get(Settings);

            if (settings.appearance.titlebar.type === "Native") {
                Window.getCurrent().setDecorations(true);
                appBuilder.register(Registration.singleton(IBackgroundService, NativeMainMenuEventHandler));
            } else {
                Window.getCurrent().setDecorations(false);
            }
        }

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
