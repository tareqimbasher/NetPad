import {IBackgroundService} from "@common";
import {IDisposable} from "aurelia";
import {IEventBus, OpenWindowCommand} from "@domain";

/**
 * This is utilized for the Web app, not the Electron app
 */
export class WebWindowBackgroundService implements IBackgroundService {
    private openWindowCommandToken: IDisposable;

    constructor(@IEventBus readonly eventBus: IEventBus) {
    }

    public start(): Promise<void> {
        this.openWindowCommandToken = this.eventBus.subscribeToServer(OpenWindowCommand, msg => {
            this.openWindow(msg);
        });
        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.openWindowCommandToken.dispose();
    }

    private openWindow(command: OpenWindowCommand) {
        let metadata = "";
        for (const key of Object.keys(command.metadata)) {
            metadata += `&${key}=${command.metadata[key]}`;
        }

        const url = window.location.origin + `?win=${command.windowName}${metadata}`;

        const options = command.options;
        const height = options.height > 1 ? options.height : screen.height * options.height;
        const width = options.width > 1 ? options.width : screen.width * options.width;

        const mainWin = window;
        const x = mainWin.top.outerWidth / 2 + mainWin.top.screenX - ( width / 2);
        const y = mainWin.top.outerHeight / 2 + mainWin.top.screenY - ( height / 2);

        let features = `width=${width},height=${height},x=${x},y=${y},location=off,toolbar=off,status=off`;

        window.open(url, command.windowName, features);

        // const focusPopup = () => { win.focus(); };
        // document.addEventListener("mousedown", focusPopup);
        // win.onclose = () => {document.removeEventListener("mousedown", focusPopup)};
        // win.addEventListener("close", () => document.removeEventListener("mousedown", focusPopup));
    }
}
