import {Dialog} from "@application/dialogs/dialog";

export interface IPromptDialogModel {
    /** Names the deed; shown in the dialog's bar. */
    title?: string;
    /** Names the value being asked for; shown as the input's label. */
    message?: string;
    defaultValue?: string;
    placeholder?: string;
}

export class PromptDialog extends Dialog<IPromptDialogModel> {
    private value: string | undefined;
    private textBox: HTMLInputElement;

    public bound() {
        this.value = this.input?.defaultValue;
    }

    protected override attached() {
        this.textBox.focus();
        // The prefilled value is a suggestion to replace, not a prefix to type after.
        this.textBox.select();
        super.attached();
    }
}
