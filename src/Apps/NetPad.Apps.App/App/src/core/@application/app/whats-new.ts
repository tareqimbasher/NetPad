import {IAppService, INotificationService} from "@application";
import {LocalStorageValue} from "@common";

/** Points the user at a release's notes the first time the app runs on a new version. */
export class WhatsNew {
    /**
     * Stores the running version and announces it when it differs from the one last stored.
     *
     * @param announce Pass false to store the version without announcing it, as on a first launch.
     */
    public static async recordVersion(
        appService: IAppService,
        notificationService: INotificationService,
        announce = true): Promise<void> {
        const current = (await appService.getIdentifier()).productVersion;
        if (!current) {
            return;
        }

        const lastSeenVersion = new LocalStorageValue<string>("netpad.whats-new.last-seen-version");
        const lastSeen = lastSeenVersion.value;
        lastSeenVersion.value = current;
        lastSeenVersion.save();

        if (!announce || !lastSeen || lastSeen === current) {
            return;
        }

        notificationService.notify(`Updated to v${current}`, "Alert", {
            link: {
                text: "See what's new",
                url: `https://github.com/tareqimbasher/NetPad/releases/tag/v${current}`
            }
        });
    }
}
