import {IAurelia, Registration} from "aurelia";
import {IBackgroundService, IIpcGateway, IWindowService} from "@application";
import {IPlatform} from "../iplatform";
import {ElectronWindowService} from "./services/electron-window-service";
import {ElectronEventSync} from "./services/electron-event-sync";
import {ElectronEventHandlerBackgroundService} from "./services/electron-event-handler-background-service";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";

/**
 * Configurations for when the app is running in Electron.
 */
export class ElectronPlatform implements IPlatform {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.singleton(IBackgroundService, ElectronEventSync),
            Registration.singleton(IBackgroundService, ElectronEventHandlerBackgroundService),
            Registration.transient(IWindowService, ElectronWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
        );
    }
}
