import {ILogger} from "aurelia";
import {DisposableCollection} from "@common";
import {AppActivatedEvent, IBackgroundService, IEventBus, OpenWindowCommand} from "@application";
import {ShellType} from "@application/windows/shell-type";
import {Window as TauriWindow} from "@tauri-apps/api/window"
import {invoke} from "@tauri-apps/api/core";

/**
 * This is utilized for the Tauri app.
 * This enables the ability to open new windows when running the Tauri app.
 */
export class TauriWindowBackgroundService implements IBackgroundService {
    private disposables = new DisposableCollection();

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @ILogger private readonly logger: ILogger) {
        this.logger = logger.scopeTo(nameof(TauriWindowBackgroundService));
    }

    public start(): Promise<void> {
        this.disposables.add(
            this.eventBus.subscribeToServer(OpenWindowCommand, msg => this.openWindow(msg))
        );

        this.listenForTitleChanges();

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.disposables.dispose();
    }

    private async openWindow(command: OpenWindowCommand) {
        const queryParams = new URLSearchParams();

        queryParams.append("win", command.windowName);
        queryParams.append("shell", ShellType.Tauri);

        for (const key of Object.keys(command.metadata)) {
            queryParams.append(key, command.metadata[key]);
        }

        const currentWindow = window;

        const url = `${currentWindow.location.origin}?${queryParams.toString()}`;

        const options = command.options;
        const height = options.height > 1 ? options.height : screen.height * options.height;
        const width = options.width > 1 ? options.width : screen.width * options.width;

        let x: number;
        let y: number;

        if (currentWindow.top) {
            x = currentWindow.top.outerWidth / 2 + currentWindow.top.screenX - (width / 2);
            y = currentWindow.top.outerHeight / 2 + currentWindow.top.screenY - (height / 2);
        } else {
            x = screen.width / 2 - (width / 2);
            y = screen.height / 2 - (height / 2);
        }

        invoke("create_window_command", {
            label: command.windowName,
            title: "",
            url: url,
            width: width,
            height: height,
            x: x,
            y: y,
        }).catch(err => {
            this.logger.error(`Error opening '${command.windowName}' window`, err);
        });
    }

    private listenForTitleChanges() {
        const setWindowTitle = async (newTitle: string | null) => {
            const win = TauriWindow.getCurrent();
            const currentTitle = await win.title()

            if (newTitle === currentTitle) {
                return;
            }

            await win.setTitle(newTitle || "");
        }

        this.eventBus.subscribeOnce(AppActivatedEvent, () => {
            const titleElement = document.querySelector("title");

            if (!titleElement) {
                return;
            }

            const observer = new MutationObserver((mutations) => {
                const newTitle = (mutations[0].target as HTMLTitleElement).innerText;
                const _ = setWindowTitle(newTitle);
            });

            observer.observe(titleElement, {subtree: true, characterData: true, childList: true});

            this.disposables.add(() => observer.disconnect());

            const _ = setWindowTitle(titleElement.innerText);
        });
    }
}
