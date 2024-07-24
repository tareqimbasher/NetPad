import {IAurelia, ILogger, Registration} from "aurelia";
import {IIpcGateway, IWindowService} from "@application";
import {IPlatform} from "../iplatform";
import {BrowserDialogBackgroundService} from "./services/browser-dialog-background-service";
import {BrowserWindowBackgroundService} from "./services/browser-window-background-service";
import {BrowserWindowService} from "./services/browser-window-service";
import {IBackgroundService} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";

/**
 * Configurations for when the app is running in the Browser.
 */
export class BrowserPlatform implements IPlatform {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.transient(IBackgroundService, BrowserDialogBackgroundService),
            Registration.transient(IBackgroundService, BrowserWindowBackgroundService),
            Registration.transient(IWindowService, BrowserWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
        );

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
