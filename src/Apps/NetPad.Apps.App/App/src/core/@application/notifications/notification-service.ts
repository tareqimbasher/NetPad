import {PLATFORM} from "aurelia";
import {WithDisposables} from "@common";
import {AppStatusMessage, AppStatusMessageKind, AppStatusMessagePublishedEvent} from "../api";
import {IEventBus} from "../events/ievent-bus";
import {ISession} from "../sessions/isession";
import {INotificationService, INotifyOptions} from "./inotification-service";
import {INotification} from "./inotification";

const STATUS_BAR_CLEAR_MS = 15000;
const MAX_HISTORY = 200;

/**
 * Routes each notification by its kind to the status bar, a toast, and/or the history.
 */
export class NotificationService extends WithDisposables implements INotificationService {
    private readonly _history: INotification[] = [];
    private readonly _toasts: INotification[] = [];
    private _statusBarMessage: INotification | null = null;
    private _unreadCount = 0;
    private paneOpen = false;

    private statusBarClearHandle: number | null = null;

    constructor(
        @IEventBus eventBus: IEventBus,
        @ISession private readonly session: ISession) {
        super();
        this.addDisposable(eventBus.subscribeToServer(
            AppStatusMessagePublishedEvent,
            ev => this.handle(ev.message.kind, this.toNotification(ev.message))));
        this.addDisposable(() => this.clearStatusBarTimer());
    }

    public get history(): ReadonlyArray<INotification> {
        return this._history;
    }

    public get toasts(): ReadonlyArray<INotification> {
        return this._toasts;
    }

    public get statusBarMessage(): INotification | null {
        return this._statusBarMessage;
    }

    public get unreadCount(): number {
        return this._unreadCount;
    }

    private handle(kind: AppStatusMessageKind, notification: INotification) {
        switch (kind) {
            case "Transient":
                this.setStatusBarMessage(notification);
                break;
            case "Notice":
                this.addToHistory(notification);
                this.setStatusBarMessage(notification);
                break;
            case "Alert":
                this.addToHistory(notification);
                this.showToast(notification);
                break;
        }
    }

    private addToHistory(notification: INotification) {
        this._history.unshift(notification);
        if (this._history.length > MAX_HISTORY) {
            // Remove the oldest which are at the end.
            this._history.splice(MAX_HISTORY);
        }
        if (!this.paneOpen) {
            this._unreadCount++;
        }
    }

    private toNotification(message: AppStatusMessage): INotification {
        return {
            scriptId: message.scriptId,
            scriptName: message.scriptId ? this.session.getScriptName(message.scriptId) : undefined,
            text: message.text,
            severity: message.severity,
            // SignalR server events bypass NSwag's fromJS, so createdDate arrives as a raw ISO string.
            createdDate: new Date(message.createdDate),
        };
    }

    private clearStatusBarTimer() {
        if (this.statusBarClearHandle !== null) {
            PLATFORM.clearTimeout(this.statusBarClearHandle);
            this.statusBarClearHandle = null;
        }
    }

    private setStatusBarMessage(notification: INotification) {
        this._statusBarMessage = notification;
        this.clearStatusBarTimer();

        this.statusBarClearHandle = PLATFORM.setTimeout(() => {
            this.statusBarClearHandle = null;
            this._statusBarMessage = null;
        }, STATUS_BAR_CLEAR_MS);
    }

    public dismissStatusBarMessage(): void {
        this.clearStatusBarTimer();
        this._statusBarMessage = null;
    }

    private showToast(notification: INotification) {
        // Alerts dwell until explicitly dismissed (there is intentionally no auto-dismiss timer).
        this._toasts.push(notification);
    }

    public notify(text: string, kind: AppStatusMessageKind, options?: INotifyOptions): void {
        const scriptId = options?.scriptId;

        this.handle(kind, {
            scriptId: scriptId,
            scriptName: scriptId ? this.session.getScriptName(scriptId) : undefined,
            text: text,
            severity: options?.severity ?? "Info",
            createdDate: new Date(),
            link: options?.link,
        });
    }

    public dismissToast(notification: INotification): void {
        const ix = this._toasts.indexOf(notification);
        if (ix >= 0) {
            this._toasts.splice(ix, 1);
        }
    }

    public removeFromHistory(notification: INotification): void {
        const ix = this._history.indexOf(notification);
        if (ix >= 0) {
            this._history.splice(ix, 1);
        }
    }

    public clearHistory(): void {
        this._history.splice(0, this._history.length);
        this._unreadCount = 0;
    }

    public setPaneOpen(open: boolean): void {
        this.paneOpen = open;
        if (open) {
            this._unreadCount = 0;
        }
    }
}
