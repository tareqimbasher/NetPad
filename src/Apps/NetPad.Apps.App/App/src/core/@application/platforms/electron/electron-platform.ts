import {IAurelia, ILogger, Registration} from "aurelia";
import {IWindowService} from "@domain";
import {IPlatform} from "../iplatform";
import {ElectronWindowService} from "./services/electron-window-service";

// Configurations for when the app is running in Electron.
export class ElectronPlatform implements IPlatform {
    configure(appBuilder: IAurelia) {
        const logger = appBuilder.container.get(ILogger).scopeTo(nameof(ElectronPlatform));
        logger.debug("Configuring for Electron platform");

        appBuilder.register(
            Registration.transient(IWindowService, ElectronWindowService),
        );
    }
}
