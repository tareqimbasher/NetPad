import {IAurelia, ILogger, Registration} from "aurelia";
import {IBackgroundService} from "@common";
import {IWindowService} from "@domain";
import {IPlatform} from "../iplatform";
import {BrowserDialogBackgroundService} from "./services/browser-dialog-background-service";
import {BrowserWindowBackgroundService} from "./services/browser-window-background-service";
import {BrowserWindowService} from "./services/browser-window-service";

/**
 * Configurations for when the app is running in the Browser.
 */
export class BrowserPlatform implements IPlatform {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.transient(IBackgroundService, BrowserDialogBackgroundService),
            Registration.transient(IBackgroundService, BrowserWindowBackgroundService),
            Registration.transient(IWindowService, BrowserWindowService),
        );

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
