import {ILogger} from "aurelia";
import {IDisposable} from "@common";
import {IBackgroundService, IEventBus, OpenWindowCommand} from "@application";
import {WindowId} from "@application/windows/window-id";
import {ShellType} from "@application/windows/shell-type";
import {Window} from "@tauri-apps/api/window"
import {WebviewWindow} from "@tauri-apps/api/webviewWindow"

/**
 * This is utilized for the Tauri app, not the Electron app
 * This enables the ability to open new windows when running the Tauri app.
 */
export class TauriWindowBackgroundService implements IBackgroundService {
    private openWindowCommandToken: IDisposable;

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @ILogger private readonly logger: ILogger) {
        this.logger = logger.scopeTo(nameof(TauriWindowBackgroundService));
    }

    public start(): Promise<void> {
        this.openWindowCommandToken = this.eventBus.subscribeToServer(OpenWindowCommand, msg => {
            const _ = this.openWindow(msg);
        });
        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.openWindowCommandToken.dispose();
    }

    private async openWindow(command: OpenWindowCommand) {
        const queryParams = new URLSearchParams();

        queryParams.append("win", command.windowName);
        queryParams.append("shell", ShellType.Tauri);

        for (const key of Object.keys(command.metadata)) {
            queryParams.append(key, command.metadata[key]);
        }

        const url = `${window.location.origin}?${queryParams.toString()}`;

        const options = command.options;
        const height = options.height > 1 ? options.height : screen.height * options.height;
        const width = options.width > 1 ? options.width : screen.width * options.width;

        let x: number;
        let y: number;

        const mainWin = window;
        if (mainWin.top) {
            x = mainWin.top.outerWidth / 2 + mainWin.top.screenX - (width / 2);
            y = mainWin.top.outerHeight / 2 + mainWin.top.screenY - (height / 2);
        } else {
            x = screen.width / 2 - (width / 2);
            y = screen.height / 2 - (height / 2);
        }

        const parent = Window.getByLabel(WindowId.Main) ?? Window.getCurrent();

        const appWindow = new WebviewWindow(command.windowName, {
            url: url,
            parent: parent,
            height: height,
            width: width,
            x: x,
            y: y,
            center: true,
        });

        const _ = appWindow.once('tauri://error', (e) => this.logger.error("appWindow error", e));
    }
}
