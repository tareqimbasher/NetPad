import {Dialog} from "@application/dialogs/dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class QuickTipsDialog extends Dialog<void> {
    private static currentVersion = "2"; // Increment this to re-trigger this popup to show as first time
    private static lsKey_LastVisitedVersion = `Dialogs.${nameof(QuickTipsDialog)}.shownForFirstTime`;
    private readonly firstUserVisit;

    public static didUserVisitLatestVersion() {
        return localStorage.getItem(QuickTipsDialog.lsKey_LastVisitedVersion) === this.currentVersion;
    }

    public static showIfFirstVisit(dialogUtil: DialogUtil) {
        if (!this.didUserVisitLatestVersion()) {
            return dialogUtil.toggle(QuickTipsDialog);
        }

        return Promise.resolve();
    }

    constructor() {
        super();
        this.firstUserVisit = !QuickTipsDialog.didUserVisitLatestVersion();
    }

    public async getStarted() {
        if (!this.firstUserVisit) {
            await this.cancel();
            return;
        }

        localStorage.setItem(QuickTipsDialog.lsKey_LastVisitedVersion, QuickTipsDialog.currentVersion);

        const divToMove = this.dialogDom.contentHost.querySelector(".quick-tips-icon") as HTMLElement;
        const destinationElement = (document.querySelector("statusbar .quick-tips-icon") as HTMLElement);

        if (!divToMove || !destinationElement) {
            await this.cancel();
            return;
        }

        // Calculate the destination position relative to the viewport
        const divToMoveRect = divToMove.getBoundingClientRect();
        const destinationRect = destinationElement.getBoundingClientRect();

        const destinationTop = destinationRect.top + window.scrollY - divToMoveRect.top + divToMoveRect.height - destinationRect.height;
        const destinationLeft = destinationRect.left + window.scrollX - divToMoveRect.left - divToMoveRect.width + destinationRect.width;

        // Move out of document flow and set to current x/y coordinates
        divToMove.style.position = "fixed";
        divToMove.style.left = divToMoveRect.x + "px";
        divToMove.style.top = divToMoveRect.y + "px";

        const movementDurationMs = 650;

        // Animate the shrinking in icon size
        divToMove.animate([
            { fontSize: "1.75rem" },
            { fontSize: "1rem" }
        ], {
            duration: movementDurationMs,
            easing: "ease-out",
            fill: "forwards"
        });

        // Animate the icon movement to the destination position
        divToMove.animate([
            {transform: `translate(${destinationLeft}px, ${destinationTop}px)`}
        ], {
            duration: movementDurationMs,
            easing: "ease-out",
            fill: "forwards"
        });

        // Fade out the icon
        divToMove.animate([
            {opacity: 1},
            {opacity: 0.3}
        ], {
            duration: movementDurationMs,
            easing: "linear"
        }).onfinish = () => {
            // Hide the icon and then close the window
            divToMove.remove();
            this.cancel();
        };
    }
}
