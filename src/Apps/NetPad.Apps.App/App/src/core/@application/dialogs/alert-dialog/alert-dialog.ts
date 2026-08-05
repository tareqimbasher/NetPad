import {Dialog} from "@application/dialogs/dialog";

export interface IAlertDialogModel {
    title?: string;
    /** Rendered as HTML, so callers may wrap a value in markup such as `<code>`. */
    message: string;
}

export class AlertDialog extends Dialog<IAlertDialogModel> {
    private okButton: HTMLButtonElement;

    protected override attached() {
        // Acknowledging is safe, so the verb takes focus and Enter fires it.
        this.okButton.focus();
        super.attached();
    }
}
