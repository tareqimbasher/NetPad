import {IAppService, INotificationService} from "@application";

const LS_KEY_LAST_SEEN_VERSION = "App.lastSeenVersion";

/** Points the user at a release's notes the first time the app runs on a new version. */
export class WhatsNew {
    /**
     * Stores the running version and, when it differs from the one last stored, announces the
     * change.
     *
     * @param announce Pass false to record the version silently — a launch that belongs to
     * onboarding has no business also reporting an update.
     */
    public static async recordVersion(
        appService: IAppService,
        notificationService: INotificationService,
        announce = true): Promise<void> {
        const current = (await appService.getIdentifier()).productVersion;
        if (!current) {
            return;
        }

        const lastSeen = localStorage.getItem(LS_KEY_LAST_SEEN_VERSION);
        localStorage.setItem(LS_KEY_LAST_SEEN_VERSION, current);

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
