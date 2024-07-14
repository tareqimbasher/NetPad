import {Constructable} from "aurelia";
import {IDialogService} from "@aurelia/dialog";
import {Dialog} from "./dialog";
import {IPromptDialogModel, PromptDialog} from "../app/prompt-dialog/prompt-dialog";

export class DialogUtil {

    constructor(@IDialogService private readonly dialogService: IDialogService) {
    }

    public prompt(options: IPromptDialogModel) {
        return this.toggle(PromptDialog, options);
    }

    public async toggle<TDialog extends typeof Dialog<TInput> | Constructable, TInput>(dialogComponent: TDialog, input?: TDialog extends typeof Dialog<infer U> ? U : unknown) {
        const key = dialogComponent.name;

        let instance = Dialog.instances.get(key);

        if (instance) {
            return instance.dialog.cancel();
        } else {
            instance = await this.dialogService.open({
                component: () => dialogComponent,
                model: input
            });

            Dialog.instances.set(key, instance);

            instance.dialog.closed.then(result => {
                Dialog.instances.delete(key);
            });

            return instance.dialog.closed;
        }
    }
}
