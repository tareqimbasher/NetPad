import {ILogger, resolve} from "aurelia";
import {IDialogDom} from "@aurelia/dialog";
import {ViewModelBase} from "@application/view-model-base";
import {OpenDialogs} from "./open-dialogs";

export abstract class Dialog<TInput> extends ViewModelBase {
    protected input?: TInput;
    protected readonly dialogDom: IDialogDom = resolve(IDialogDom);

    constructor() {
        super(resolve(ILogger));
        this.dialogDom.contentHost.classList.add("dialog");
        this.dialogDom.overlay.classList.add("dialog-overlay");
    }

    protected activate(input?: TInput) {
        this.input = input;
    }

    protected override attaching() {
        super.attaching();

        // Animate the parent so the overlay is included in the opacity animation
        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "0"}, {opacity: "1"}], {
            duration: 200,
        });

        return animation?.finished;
    }

    protected override detaching() {
        super.detaching();

        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "1"}, {opacity: "0"}], {
            duration: 200,
        });

        return animation?.finished;
    }

    protected async ok(value?: unknown, event?: Event) {
        if (event) {
            event.preventDefault();
        }

        const instance = OpenDialogs.get(this.constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.ok(value);
    }

    protected async cancel() {
        const instance = OpenDialogs.get(this.constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.cancel();
    }
}
