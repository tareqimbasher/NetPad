import {IAurelia, Registration} from "aurelia";
import {IBackgroundService} from "@common";
import {IWindowService} from "@domain";
import {IPlatform} from "../iplatform";
import {ElectronWindowService} from "./services/electron-window-service";
import {ElectronEventSync} from "./services/electron-event-sync";
import {ElectronEventHandlerBackgroundService} from "./services/electron-event-handler-background-service";

/**
 * Configurations for when the app is running in Electron.
 */
export class ElectronPlatform implements IPlatform {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.singleton(IBackgroundService, ElectronEventSync),
            Registration.singleton(IBackgroundService, ElectronEventHandlerBackgroundService),
            Registration.transient(IWindowService, ElectronWindowService),
        );
    }
}
