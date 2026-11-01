import {Dialog} from "@application/dialogs/dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {CommandIds} from "@application/commands/command-ids";
import {IKeybindingManager} from "@application/keybindings/ikeybinding-manager";
import {migrateLocalStorageKey} from "@common/data/local-storage-key";

const LS_KEY_SEEN = "netpad.quick-start.seen";

/**
 * The onboarding dialog, shown on first launch.
 */
export class QuickStartDialog extends Dialog<void> {
    private getStartedButton: HTMLButtonElement;

    /** Key caps for the commands the tour points at, so a rebound key shows here as rebound. */
    private readonly caps: Record<string, string[]>;

    public static isFirstVisit() {
        migrateLocalStorageKey("Dialogs.QuickTipsDialog.shownForFirstTime", LS_KEY_SEEN);
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
