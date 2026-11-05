import {WithDisposables} from "@common";
import {IBackgroundService, INotificationService} from "@application";
import {listen} from "@tauri-apps/api/event";

/**
 * Tells the user where a file the webview downloaded, such as an export, was saved.
 */
export class TauriDownloadNoticeBackgroundService extends WithDisposables implements IBackgroundService {
    constructor(@INotificationService private readonly notificationService: INotificationService) {
        super();
    }

    public async start(): Promise<void> {
        const unlisten = await listen<string>("download-finished", event => {
            const savedTo = event.payload ? `Saved to ${event.payload}` : "Saved to your Downloads folder";
            this.notificationService.notify(savedTo, "Notice", {severity: "Success"});
        });

        this.addDisposable(unlisten);
    }

    public stop(): void {
        this.dispose();
    }
}
