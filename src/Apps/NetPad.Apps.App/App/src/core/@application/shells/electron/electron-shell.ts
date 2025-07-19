import {IAurelia, Registration} from "aurelia";
import {IBackgroundService, IIpcGateway, IWindowService, Settings} from "@application";
import {IShell} from "../ishell";
import {ElectronWindowService} from "./services/electron-window-service";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {ElectronEventSync} from "./services/electron-event-sync";
import {NativeMainMenuEventHandler} from "./services/native-main-menu-event-handler";
import {WindowParams} from "@application/windows/window-params";
import {WindowId} from "@application/windows/window-id";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {ElectronNativeDialogService} from "@application/shells/electron/services/electron-native-dialog-service";

/**
 * Configurations for when the app is running in Electron.
 */
export class ElectronShell implements IShell {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.singleton(IBackgroundService, ElectronEventSync),
            Registration.transient(IWindowService, ElectronWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
            Registration.singleton(INativeDialogService, ElectronNativeDialogService),
        );

        const settings = appBuilder.container.get(Settings);

        if (settings.appearance.titlebar.type === "Native" && WindowParams.window === WindowId.Main) {
            appBuilder.register(Registration.singleton(IBackgroundService, NativeMainMenuEventHandler));
        }
    }
}
