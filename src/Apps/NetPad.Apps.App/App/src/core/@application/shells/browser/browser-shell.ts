import {IAurelia, ILogger, Registration} from "aurelia";
import {IIpcGateway, IWindowService} from "@application";
import {IShell} from "../ishell";
import {BrowserDialogBackgroundService} from "./services/browser-dialog-background-service";
import {BrowserWindowBackgroundService} from "./services/browser-window-background-service";
import {BrowserWindowService} from "./services/browser-window-service";
import {IBackgroundService} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {WindowParams} from "@application/windows/window-params";

/**
 * Configurations for when the app is running in the Browser.
 */
export class BrowserShell implements IShell {
    public configure(appBuilder: IAurelia, windowParams: WindowParams) {
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
