import {INotification, INotificationService, ISession, Pane, severityIconClass, severityTextClass} from "@application";
import {resolve} from "aurelia";
import {watch} from "@aurelia/runtime-html";

export class NotificationsPane extends Pane {
    public readonly notificationService: INotificationService = resolve(INotificationService);
    private readonly session: ISession = resolve(ISession);

    public readonly iconClass = severityIconClass;
    public readonly textClass = severityTextClass;

    constructor() {
        super("Notifications", "notifications-icon");
    }

    public override get badgeCount(): number {
        return this.notificationService.unreadCount;
    }

    @watch<NotificationsPane>(vm => vm.isOpen)
    private paneOpenChanged(): void {
        this.notificationService.setPaneOpen(this.isOpen);
    }

    public async navigateTo(item: INotification): Promise<void> {
        if (item.scriptId && this.session.isScriptOpen(item.scriptId)) {
            await this.session.activate(item.scriptId);
        }
    }

    public dismiss(item: INotification, event: Event): void {
        event.stopPropagation();
        this.notificationService.removeFromHistory(item);
    }

    public clearAll(): void {
        this.notificationService.clearHistory();
    }
}
