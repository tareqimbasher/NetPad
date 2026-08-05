import {Dialog} from "@application/dialogs/dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {CommandIds} from "@application/commands/command-ids";
import {IKeybindingManager} from "@application/keybindings/ikeybinding-manager";

// Frozen: the key predates the dialog's rename, and changing it would re-onboard every existing
// install.
const LS_KEY_SEEN = "Dialogs.QuickTipsDialog.shownForFirstTime";

/**
 * Onboarding, shown once. Version-change announcements are the what's-new toast's job, not this
 * dialog's.
 */
export class QuickStartDialog extends Dialog<void> {
    private getStartedButton: HTMLButtonElement;

    /** Key caps for the commands the tour points at, so a rebound key stays honest here. */
    private readonly caps: Record<string, string[]>;

    public static isFirstVisit() {
        return !localStorage.getItem(LS_KEY_SEEN);
    }

    public static show(dialogUtil: DialogUtil) {
        localStorage.setItem(LS_KEY_SEEN, "true");
        return dialogUtil.open(QuickStartDialog);
    }

    constructor(@IKeybindingManager keybindingManager: IKeybindingManager) {
        super();

        const capsFor = (commandId: string) =>
            keybindingManager.getKeybinding(commandId)?.keyCombo.asCaps().flat() ?? [];

        this.caps = {
            run: capsFor(CommandIds.runScript),
            palette: capsFor(CommandIds.openCommandPalette),
            settings: capsFor(CommandIds.openSettings),
        };
    }

    protected override attached() {
        this.getStartedButton.focus();
        super.attached();
    }
}
