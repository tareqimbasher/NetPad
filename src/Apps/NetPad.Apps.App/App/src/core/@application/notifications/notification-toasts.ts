import {INotificationService} from "./inotification-service";
import {INotification} from "./inotification";
import {severityIconClass, severityTextClass} from "./notification-appearance";

/**
 * Renders active toasts. Backed by {@link INotificationService}.
 */
export class NotificationToasts {
    public readonly iconClass = severityIconClass;
    public readonly textClass = severityTextClass;

    constructor(@INotificationService readonly notificationService: INotificationService) {
    }

    public dismiss(notification: INotification) {
        this.notificationService.dismissToast(notification);
    }
}
