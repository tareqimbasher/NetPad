import {IPlatform} from "@application/platforms/iplatform";
import {IAurelia, ILogger} from "aurelia";

// Configurations for when the Electron app is running
export class ElectronPlatform implements IPlatform {
    configure(appBuilder: IAurelia) {
        const logger = appBuilder.container.get(ILogger).scopeTo(nameof(ElectronPlatform));
        logger.debug("Configuring for Electron platform");
    }
}
