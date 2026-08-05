import {ILogger, resolve} from "aurelia";
import {IDialogDom} from "@aurelia/dialog";
import {ViewModelBase} from "@application/view-model-base";
import {OpenDialogs} from "./open-dialogs";

const FADE_MS = 180;

const FOCUSABLE = [
    "a[href]",
    "button:not([disabled])",
    "input:not([disabled])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "[tabindex]:not([tabindex='-1'])",
].join(",");

export abstract class Dialog<TInput> extends ViewModelBase {
    protected input?: TInput;
    protected readonly dialogDom: IDialogDom = resolve(IDialogDom);

    constructor() {
        super(resolve(ILogger));
        this.dialogDom.contentHost.classList.add("dialog");
        this.dialogDom.overlay.classList.add("dialog-overlay");
        this.dialogDom.contentHost.setAttribute("role", "dialog");
        this.dialogDom.contentHost.setAttribute("aria-modal", "true");
        // So the dialog can hold focus itself when nothing inside it should: the keyboard then
        // reaches the overlay rather than the window behind it.
        this.dialogDom.contentHost.tabIndex = -1;
    }

    protected activate(input?: TInput) {
        this.input = input;
    }

    protected override attaching() {
        super.attaching();

        if (!Dialog.motionAllowed) {
            return undefined;
        }

        // Animate the parent so the overlay is included in the opacity animation
        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "0"}, {opacity: "1"}], {
            duration: FADE_MS,
        });

        return animation?.finished;
    }

    protected attached() {
        this.dialogDom.contentHost.addEventListener("keydown", this.keepFocusInDialog);
        this.addDisposable(() =>
            this.dialogDom.contentHost.removeEventListener("keydown", this.keepFocusInDialog));

        // A dialog that focused nothing of its own still has to own the keyboard.
        if (!this.dialogDom.contentHost.contains(document.activeElement)) {
            this.dialogDom.contentHost.focus();
        }
    }

    protected override detaching() {
        super.detaching();

        if (!Dialog.motionAllowed) {
            return undefined;
        }

        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "1"}, {opacity: "0"}], {
            duration: FADE_MS,
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

    private static get motionAllowed() {
        return !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    }

    private readonly keepFocusInDialog = (event: KeyboardEvent) => {
        if (event.key !== "Tab") {
            return;
        }

        const host = this.dialogDom.contentHost;
        const focusable = Array.from(host.querySelectorAll<HTMLElement>(FOCUSABLE))
            .filter(el => el.offsetParent !== null);

        const first = focusable[0] ?? host;
        const last = focusable[focusable.length - 1] ?? host;
        const wrapTo = event.shiftKey
            ? (document.activeElement === first || document.activeElement === host ? last : null)
            : (document.activeElement === last || document.activeElement === host ? first : null);

        if (wrapTo) {
            event.preventDefault();
            wrapTo.focus();
        }
    };
}
