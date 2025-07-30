import {IAurelia, ILogger, Registration} from "aurelia";
import {IIpcGateway, IWindowService} from "@application";
import {IShell} from "../ishell";
import {IBackgroundService} from "@application";
import {SignalRIpcGateway} from "@application/events/signalr-ipc-gateway";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {BrowserWindowService} from "./services/browser-window-service";
import {BrowserNativeDialogService} from "./services/browser-native-dialog-service";
import {BrowserWindowBackgroundService} from "./services/browser-window-background-service";
import {BrowserDialogBackgroundService} from "./services/browser-dialog-background-service";

/**
 * Configurations for when the app is running in the Browser.
 */
export class BrowserShell implements IShell {
    public configure(appBuilder: IAurelia) {
        appBuilder.register(
            Registration.transient(IBackgroundService, BrowserDialogBackgroundService),
            Registration.transient(IBackgroundService, BrowserWindowBackgroundService),
            Registration.transient(IWindowService, BrowserWindowService),
            Registration.singleton(IIpcGateway, SignalRIpcGateway),
            Registration.singleton(INativeDialogService, BrowserNativeDialogService),
        );

        // Disable default right-click action
        document.addEventListener("contextmenu", (ev) => {
            ev.preventDefault();
            return false;
        });
    }
}
