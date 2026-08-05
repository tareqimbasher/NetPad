import {IDisposable} from "@common";
import {IBackgroundService, IEventBus, OpenWindowCommand} from "@application";
import {WindowId} from "@application/windowing/window-id";
import {WindowParams} from "@application/windowing/window-params";

/**
 * This is utilized for the Browser app, not the Electron app
 * This enables the ability to open new windows when running the browser app.
 */
export class BrowserWindowBackgroundService implements IBackgroundService {
    private openWindowCommandToken?: IDisposable;
    private readonly openedWindows = new Map<string, Window>();

    constructor(@IEventBus readonly eventBus: IEventBus) {
    }

    public start(): Promise<void> {
        this.openWindowCommandToken = this.eventBus.subscribeToServer(OpenWindowCommand, msg => {
            this.openWindow(msg);
        });
        return Promise.resolve(undefined);
    }

    public stop(): void {
        if (this.openWindowCommandToken) {
            this.openWindowCommandToken.dispose();
        }
    }

    private openWindow(command: OpenWindowCommand) {
        // The command is broadcast to every window, but only the main window acts on it.
        if (WindowParams.window !== WindowId.Main) {
            return;
        }

        const alreadyOpen = this.openedWindows.get(command.windowName);
        if (alreadyOpen && !alreadyOpen.closed) {
            alreadyOpen.focus();
            return;
        }

        let metadata = "";
        for (const key of Object.keys(command.metadata)) {
            metadata += `&${key}=${command.metadata[key]}`;
        }

        const url = window.location.origin + `?win=${command.windowName}&token=${WindowParams.token || ""}${metadata}`;

        const options = command.options;
        // A size over 1 is in pixels, a size under it is a share of the screen. Size is capped so it
        // always fits the screen.
        const height = Math.min(options.height > 1 ? options.height : screen.height * options.height, screen.height * 0.92);
        const width = Math.min(options.width > 1 ? options.width : screen.width * options.width, screen.width * 0.92);

        const mainWin = window;
        if (!mainWin.top) {
            return;
        }

        const x = mainWin.top.outerWidth / 2 + mainWin.top.screenX - (width / 2);
        const y = mainWin.top.outerHeight / 2 + mainWin.top.screenY - (height / 2);

        const features = `width=${width},height=${height},x=${x},y=${y},location=no,status=no,toolbar=no,menubar=no,resizable=yes,titlebar=no`;

        const opened = window.open(url, command.windowName, features);
        if (opened) {
            this.openedWindows.set(command.windowName, opened);
        }
    }
}
