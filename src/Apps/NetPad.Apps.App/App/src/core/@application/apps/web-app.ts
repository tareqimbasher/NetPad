import Aurelia, {Registration} from "aurelia";
import {IBackgroundService} from "@common";
import {WebDialogBackgroundService, WebWindowBackgroundService} from "@application";

export class WebApp {
    // Adds services and code specific to when the WebApp (browser) is running
    public static configure(app: Aurelia) {
        app.register(
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
