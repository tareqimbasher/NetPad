import {resolve, ILogger, Constructable} from "aurelia";
import {ViewModelBase} from "@application/view-model-base";
import {DialogOpenResult, IDialogDom, IDialogService} from "@aurelia/dialog";

export abstract class Dialog<TInput> extends ViewModelBase {
    public static instances = new Map<string, DialogOpenResult>();
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

    protected async ok(value?: unknown) {
        const instance = Dialog.instances.get((this as Record<string, unknown>).constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.ok(value);
    }

    protected async cancel() {
        const instance = Dialog.instances.get((this as Record<string, unknown>).constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.cancel();
    }

    public async toggle<TDialog extends typeof Dialog<TOptions> | Constructable, TOptions>(
        dialogService: IDialogService,
        dialogComponent: TDialog,
        model?: unknown) {

        const key = dialogComponent.name;

        let instance = Dialog.instances.get(key);

        if (instance) {
            return instance.dialog.cancel();
        } else {
            instance = await dialogService.open({
                component: () => dialogComponent,
                model: model
            });

            Dialog.instances.set(key, instance);

            instance.dialog.closed.then(result => {
                Dialog.instances.delete(key);
            });

            return instance.dialog.closed;
        }
    }
}
