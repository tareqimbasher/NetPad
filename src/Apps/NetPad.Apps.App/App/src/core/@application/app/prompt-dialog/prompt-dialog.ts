import {Dialog} from "@application/dialogs/dialog";

export interface IPromptDialogModel {
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

    public attached() {
        this.textBox.focus();
    }
}
