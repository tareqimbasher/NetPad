import {ViewModelBase} from "@application/view-model-base";
import {Constructable, ILogger} from "aurelia";
import {DialogOpenResult, IDialogDom, IDialogService} from "@aurelia/dialog";

export abstract class DialogBase extends ViewModelBase {
    private static instances = new Map<string, DialogOpenResult>();

    protected constructor(
        protected readonly dialogDom: IDialogDom,
        logger: ILogger
    ) {
        super(logger);
        dialogDom.contentHost.classList.add("dialog");
        dialogDom.overlay.classList.add("dialog-overlay");
    }

    public override attaching() {
        super.attaching();

        // Animate the parent so the overlay is included in the opacity animation
        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "0"}, {opacity: "1"}], {
            duration: 200,
        });

        return animation?.finished;
    }

    public override detaching() {
        super.detaching();

        const animation = this.dialogDom.contentHost.parentElement?.animate([{opacity: "1"}, {opacity: "0"}], {
            duration: 200,
        });

        return animation?.finished;
    }

    public static async toggle<TDialog extends Constructable>(
        dialogService: IDialogService,
        dialogComponent: TDialog,
        model?: unknown) {

        const key = dialogComponent.name;

        let instance = DialogBase.instances.get(key);

        if (instance) {
            return instance.dialog.cancel();
        } else {
            instance = await dialogService.open({
                component: () => dialogComponent,
                model: model
            });

            this.instances.set(key, instance);

            instance.dialog.closed.then(result => {
                this.instances.delete(key);
            });

            return instance.dialog.closed;
        }
    }

    public async ok(value?: unknown) {
        const instance = DialogBase.instances.get((this as Record<string, unknown>).constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.ok(value);
    }

    public async cancel() {
        const instance = DialogBase.instances.get((this as Record<string, unknown>).constructor.name);

        if (!instance) {
            return undefined;
        }

        return instance.dialog.cancel();
    }
}
