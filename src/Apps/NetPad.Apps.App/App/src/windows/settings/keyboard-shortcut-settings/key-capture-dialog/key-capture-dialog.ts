import {KeyCombo} from "@application";
import {Dialog} from "@application/dialogs/dialog";

export interface IKeyCaptureDialogModel {
    /** The combination the shortcut has now; unbound when it has none. */
    current: KeyCombo;

    /** Names the shortcut that already uses a combination, if any other does. */
    findConflict: (combo: KeyCombo) => string | undefined;
}

/**
 * Captures a new key combination for a shortcut. Closes with the combination when confirmed.
 */
export class KeyCaptureDialog extends Dialog<IKeyCaptureDialogModel> {
    /**
     * Keys a user may assign without holding a modifier.
     */
    private static readonly allowedStandaloneKeys = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
    ];

    private pressedKeyCombo?: KeyCombo;
    private conflictingShortcutName?: string;

    public get isKeyComboValid(): boolean {
        return !!this.pressedKeyCombo?.key
            && (this.pressedKeyCombo.hasModifier
                || KeyCaptureDialog.allowedStandaloneKeys.includes(this.pressedKeyCombo.key))
            && !this.conflictingShortcutName;
    }

    public get isKeyComboIncomplete(): boolean {
        return !!this.pressedKeyCombo && !this.pressedKeyCombo.key;
    }

    protected override attached() {
        // Every key is a candidate, Tab and Enter included, so the capture runs ahead of the
        // dialog's own focus handling and of whatever element inside it holds focus.
        const host = this.dialogDom.contentHost;
        host.addEventListener("keydown", this.capture, true);
        this.addDisposable(() => host.removeEventListener("keydown", this.capture, true));

        super.attached();
    }

    public async confirm() {
        if (!this.isKeyComboValid || !this.pressedKeyCombo) return;

        await this.ok(this.pressedKeyCombo.clone());
    }

    private readonly capture = (event: KeyboardEvent) => {
        event.preventDefault();
        event.stopImmediatePropagation();

        if (event.key === "Escape") {
            void this.cancel();
            return;
        }

        this.pressedKeyCombo = KeyCombo.fromKeyboardEvent(event);
        this.conflictingShortcutName = this.pressedKeyCombo.isBound
            ? this.input?.findConflict(this.pressedKeyCombo)
            : undefined;
    };
}
