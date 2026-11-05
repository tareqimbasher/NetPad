import {Dialog} from "@application/dialogs/dialog";

export interface IPromptDialogModel {
    /** Shown in the dialog's title bar. */
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
        this.textBox.select();
        super.attached();
    }
}
