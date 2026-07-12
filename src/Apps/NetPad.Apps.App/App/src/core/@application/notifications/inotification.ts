import {AppStatusMessageSeverity} from "../api";

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
}
