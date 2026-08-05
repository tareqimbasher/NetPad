import {AppStatusMessageSeverity} from "../api";

/**
 * Where a notification can send the user; rendered as a link in the toast and the history row.
 */
export interface INotificationLink {
    readonly text: string;
    /** Opened in the user's browser, so an external address rather than an in-app route. */
    readonly url: string;
}

/**
 * A notification displayed by the UI, originating from a server AppStatusMessage
 * or raised client-side via INotificationService.notify().
 */
export interface INotification {
    readonly scriptId?: string;
    scriptName?: string;
    readonly text: string;
    readonly severity: AppStatusMessageSeverity;
    readonly createdDate: Date;
    readonly link?: INotificationLink;
}
