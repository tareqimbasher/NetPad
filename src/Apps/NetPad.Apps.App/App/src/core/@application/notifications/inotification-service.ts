import {DI} from "aurelia";
import {INotification} from "./inotification";
import {AppStatusMessageKind, AppStatusMessageSeverity} from "@application/api";

export const INotificationService = DI.createInterface<INotificationService>();

/** Central store of the app's notifications. */
export interface INotificationService {
    /** Newest-first record of notifications kept for later review. */
    readonly history: ReadonlyArray<INotification>;

    /** Toasts currently on screen. */
    readonly toasts: ReadonlyArray<INotification>;

    /** The message currently shown in the status bar. */
    readonly statusBarMessage: INotification | null;

    /** Number of history items the user has not yet seen. */
    readonly unreadCount: number;

    /** Publishes a notification. */
    notify(text: string, kind: AppStatusMessageKind, severity?: AppStatusMessageSeverity, scriptId?: string): void;

    /** Removes a toast. Does not remove it from the history. */
    dismissToast(notification: INotification): void;

    /** Clears the current status bar message. */
    dismissStatusBarMessage(): void;

    /** Removes a single item from the history. */
    removeFromHistory(notification: INotification): void;

    /** Clears the entire history. */
    clearHistory(): void;

    /** Tracks whether the history is currently visible to the user. While visible, items aren't counted as unread. */
    setPaneOpen(open: boolean): void;
}
