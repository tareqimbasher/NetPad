import {IAurelia, Registration} from "aurelia";
import {IShell} from "../ishell";
import {IBackgroundService, IIpcGateway, IWindowService, Settings} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {NativeMainMenuEventHandler} from "./services/native-main-menu-event-handler";
import {Window} from "@tauri-apps/api/window"
import {WindowId} from "@application/windows/window-id";
import {WindowParams} from "@application/windows/window-params";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {TauriWindowService} from "./services/tauri-window-service";
import {TauriNativeDialogService} from "./services/tauri-native-dialog-service";
import {TauriWindowBackgroundService} from "./services/tauri-window-background-service";
import {TauriDialogBackgroundService} from "./services/tauri-dialog-background-service";
import {TauriDragDropBackgroundService} from "./services/tauri-drag-drop-background-service";

export class TauriShell implements IShell {
    public configure(appBuilder: IAurelia): void {
        appBuilder.register(
            Registration.singleton(IBackgroundService, TauriDialogBackgroundService),
            Registration.singleton(IBackgroundService, TauriWindowBackgroundService),
            Registration.transient(IWindowService, TauriWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
            Registration.singleton(INativeDialogService, TauriNativeDialogService),
        );

        if (WindowParams.window === WindowId.Main) {
            appBuilder.register(Registration.singleton(IBackgroundService, TauriDragDropBackgroundService));

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
