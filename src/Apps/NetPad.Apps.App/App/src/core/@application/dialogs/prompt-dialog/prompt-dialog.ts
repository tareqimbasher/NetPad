import {IDialogDom} from "@aurelia/dialog";
import {DialogBase} from "../dialog-base";
import {ILogger} from "aurelia";

export interface IPromptDialogModel {
    message?: string;
    defaultValue?: string;
    placeholder?: string;
}

export class PromptDialog extends DialogBase {
    private model: IPromptDialogModel;
    private value: string | undefined;
    private textBox: HTMLInputElement;

    constructor(@IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger) {
        super(dialogDom, logger);
    }

    public activate(model: IPromptDialogModel) {
        this.model = model;
        this.value = model.defaultValue;
    }

    public attached() {
        this.textBox.focus();
    }
}
