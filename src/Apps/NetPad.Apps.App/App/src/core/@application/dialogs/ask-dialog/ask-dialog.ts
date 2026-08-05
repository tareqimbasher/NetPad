import {Dialog} from "@application/dialogs/dialog";

export interface IAskDialogModel {
    title?: string;
    /** Rendered as HTML, so callers may wrap a value in markup such as `<code>`. */
    message: string;
    buttons?: IAskDialogButton[];
}

export interface IAskDialogButton {
    /**
     * The button text. Will be returned as the value of the result if the button is selected and value is undefined.
     */
    text: string;

    /** The value of the result if the button is selected. */
    value?: string | null;

    /**
     * Whether the button is a primary button. An irreversible deed gets no primary at all — its
     * verb carries `btn-danger` instead, which leaves Enter inert.
     */
    isPrimary?: boolean;

    /** Additional CSS classes to add to the button. */
    cssClasses?: string;
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
            // The operative verb sits rightmost, the way every footer in the app reads.
            this.input.buttons = [
                {
                    text: "Cancel",
                },
                {
                    text: "OK",
                    isPrimary: true
                }
            ];
        }
    }

    protected override attached() {
        const primaryButtons = this.element.querySelectorAll(".window-footer [data-is-primary=true]");

        if (primaryButtons.length) {
            (primaryButtons[0] as HTMLButtonElement).focus();
        }

        super.attached();
    }
}
