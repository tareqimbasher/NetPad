import {IDialogDom} from "@aurelia/runtime-html";
import {ViewModelBase} from "@application/view-model-base";
import {Constructable, DialogOpenResult, IDialogService, ILogger} from "aurelia";

export abstract class DialogBase extends ViewModelBase {
    private static instances = new Map<string, DialogOpenResult>();

    protected constructor(
        @IDialogDom protected readonly dialogDom: IDialogDom,
        @ILogger logger: ILogger
    ) {
        super(logger);
        dialogDom.contentHost.classList.add("dialog");
        dialogDom.overlay.style.backgroundColor = "rgba(0, 0, 0, 0.5)";
    }

    public attaching() {
        const animation = this.dialogDom.contentHost.animate([{opacity: "0"}, {opacity: "1"}], {
            duration: 100,
        });

        return animation.finished;
    }

    public override detaching() {
        super.detaching();

        const animation = this.dialogDom.contentHost.animate([{opacity: "1"}, {opacity: "0"}], {
            duration: 100,
        });

        return animation.finished;
    }

    public static async toggle<TDialog extends Constructable>(dialogService: IDialogService, dialogComponent: TDialog, model?: unknown) {
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

    public async close() {
        const instance = DialogBase.instances.get((this as Record<string, unknown>).constructor.name);

        if (!instance) {
            return;
        }

        return instance.dialog.cancel();
    }
}
