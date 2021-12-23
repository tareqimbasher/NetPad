import {DialogBase, IShortcutManager, Shortcut} from "@domain";
import {IDialogDom} from "@aurelia/runtime-html";
import {IDialogController, IDialogService} from "aurelia";

export class KeyboardShortcutsDialog extends DialogBase {
    public shortcuts: Shortcut[];
    private static openedDialog: IDialogController | null = null;

    constructor(@IDialogDom dialogDom: IDialogDom,
                @IShortcutManager readonly shortcutManager: IShortcutManager) {
        super(dialogDom);
    }

    public override attaching(): Promise<Animation> {
        this.shortcuts = this.shortcutManager.getRegisteredShortcuts();
        return super.attaching();
    }

    public async close() {
        await KeyboardShortcutsDialog.close();
    }

    public static async toggle(dialogService: IDialogService) {
        if (this.openedDialog) {
            await this.close();
            return;
        }

        const opened = await dialogService.open({
            component: () => KeyboardShortcutsDialog,
        });

        this.openedDialog = opened.dialog;
        await opened.dialog.closed;
        this.openedDialog = null;
    }

    public static async close() {
        await this.openedDialog.cancel();
        this.openedDialog = null;
    }
}
