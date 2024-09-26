import {Dialog} from "@application/dialogs/dialog";

export interface IAskDialogModel {
    title?: string;
    message: string;
    buttons?: IAskDialogButton[];
}

export interface IAskDialogButton {
    /**
     * The button text. Will be returned as the value of the result if the button is selected and value is undefined.
     */
    text: string;

    /**
     * The value of the result if the button is selected.
     */
    value?: string | null;

    /**
     * Whether the button is a primary button.
     */
    isPrimary?: boolean;
}

export class AskDialog extends Dialog<IAskDialogModel> {
    constructor(private readonly element: Element) {
        super();
    }

    public bound() {
        if (!this.input) {
            throw new Error("No input provided to AskDialog");
        }

        if (!this.input?.buttons || !this.input.buttons.length) {
            this.input.buttons = [
                {
                    text: "OK",
                    isPrimary: true
                },
                {
                    text: "Cancel",
                }
            ];
        }
    }

    public attached() {
        const primaryButtons = this.element.querySelectorAll(".buttons [data-is-primary=true]");

        if (primaryButtons.length) {
            (primaryButtons[0] as HTMLButtonElement).focus();
        }
    }
}
