import {ILogger} from "aurelia";
import {IDialogDom, IDialogService} from "@aurelia/dialog";
import {DialogBase} from "@application/dialogs/dialog-base";

export class QuickTipsDialog extends DialogBase {
    private static currentVersion = "2"; // Increment this to re-trigger this popup to show as first time
    private static lsKey_LastVisitedVersion = `Dialogs.${nameof(QuickTipsDialog)}.shownForFirstTime`;
    private readonly firstUserVisit;

    public static didUserVisitLatestVersion() {
        return localStorage.getItem(QuickTipsDialog.lsKey_LastVisitedVersion) === this.currentVersion;
    }

    public static showIfFirstVisit(dialogService: IDialogService) {
        if (!this.didUserVisitLatestVersion()) {
            return super.toggle(dialogService, QuickTipsDialog);
        }

        return Promise.resolve();
    }

    constructor(@IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger) {
        super(dialogDom, logger);

        this.firstUserVisit = !QuickTipsDialog.didUserVisitLatestVersion();
    }

    public async getStarted() {
        if (!this.firstUserVisit) {
            await this.close();
            return;
        }

        localStorage.setItem(QuickTipsDialog.lsKey_LastVisitedVersion, QuickTipsDialog.currentVersion);

        const divToMove = this.dialogDom.contentHost.querySelector('.fa-star') as HTMLElement;
        const destinationElement = (document.querySelector("statusbar .info-icon.action-icon") as HTMLElement);

        if (!divToMove || !destinationElement) {
            await this.close();
            return;
        }

        const movementDurationMs = 650;
        const fadeOutDurationMs = 300;

        // Calculate the destination position relative to the viewport
        const divToMoveRect = divToMove.getBoundingClientRect();
        const destinationRect = destinationElement.getBoundingClientRect();

        const destinationTop = destinationRect.top + window.scrollY - divToMoveRect.top - 4;
        const destinationLeft = destinationRect.left + window.scrollX - divToMoveRect.left;

        // Change the icon
        setTimeout(() => {
            divToMove.classList.remove("text-yellow");
            divToMove.classList.remove("fa-star");
            divToMove.classList.add("info-icon");
        }, movementDurationMs - (movementDurationMs * 0.33));

        // Animate the shrinking in icon size
        divToMove.animate([
            { fontSize: "1.75rem" },
            { fontSize: "1rem" }
        ], {
            duration: movementDurationMs,
            easing: "ease-out"
        }).onfinish = () => {
            divToMove.style.fontSize = "1rem";
        };

        // Animate the icon movement to the destination position
        divToMove.animate([
            {transform: `translate(${divToMove.offsetLeft}px, ${divToMove.offsetTop}px)`},
            {transform: `translate(${destinationLeft}px, ${destinationTop}px)`}
        ], {
            duration: movementDurationMs,
            easing: "ease-out"
        }).onfinish = () => {
            // Set the icon's position to the destination position
            divToMove.style.transform = `translate(${destinationLeft}px, ${destinationTop}px)`;

            // Fade out the icon
            divToMove.animate([
                {opacity: 1},
                {opacity: 0}
            ], {
                duration: fadeOutDurationMs,
                easing: "linear"
            }).onfinish = () => {
                // Hide the icon and then close the window
                divToMove.style.display = "none";
                this.close();
            };
        };
    }
}
