import {Dialog} from "@application/dialogs/dialog";
import {DialogMessage} from "@application/dialogs/dialog-message";

export type IAlertDialogModel = DialogMessage & {
    title?: string;
};

export class AlertDialog extends Dialog<IAlertDialogModel> {
    private okButton: HTMLButtonElement;

    protected override attached() {
        this.okButton.focus();
        super.attached();
    }
}
