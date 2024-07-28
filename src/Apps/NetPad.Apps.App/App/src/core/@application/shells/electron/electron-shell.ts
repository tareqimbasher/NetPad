import {IAurelia, Registration} from "aurelia";
import {IBackgroundService, IIpcGateway, IWindowService} from "@application";
import {IShell} from "../ishell";
import {ElectronWindowService} from "./services/electron-window-service";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {ElectronEventSync} from "./services/electron-event-sync";
import {NativeMainMenuEventHandler} from "./services/native-main-menu-event-handler";

/**
 * Configurations for when the app is running in Electron.
 */
export class ElectronShell implements IShell {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.singleton(IBackgroundService, ElectronEventSync),
            Registration.singleton(IBackgroundService, NativeMainMenuEventHandler),
            Registration.transient(IWindowService, ElectronWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
        );
    }
}
