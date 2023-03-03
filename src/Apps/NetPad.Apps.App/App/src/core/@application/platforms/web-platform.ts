import {IAurelia, ILogger, Registration} from "aurelia";
import {IBackgroundService} from "@common";
import {WebDialogBackgroundService, WebWindowBackgroundService} from "@application";
import {IPlatform} from "@application/platforms/iplatform";

// Configurations for when the WebApp (browser) is running
export class WebPlatform implements IPlatform {
    public configure(appBuilder: IAurelia) {
        const logger = appBuilder.container.get(ILogger).scopeTo(nameof(WebPlatform));
        logger.debug("Configuring for web platform");

        appBuilder.register(
            Registration.transient(IBackgroundService, WebDialogBackgroundService),
            Registration.transient(IBackgroundService, WebWindowBackgroundService)
        );

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
