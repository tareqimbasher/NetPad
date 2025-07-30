import {IAurelia, Registration} from "aurelia";
import {IBackgroundService, IIpcGateway, IWindowService, Settings} from "@application";
import {IShell} from "../ishell";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {WindowParams} from "@application/windows/window-params";
import {WindowId} from "@application/windows/window-id";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {ElectronWindowService} from "./services/electron-window-service";
import {ElectronNativeDialogService} from "./services/electron-native-dialog-service";
import {NativeMainMenuEventHandler} from "./services/native-main-menu-event-handler";
import {ElectronEventSync} from "./services/electron-event-sync";

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
