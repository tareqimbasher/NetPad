import {IDialogService} from "@aurelia/dialog";
import {DialogBase} from "@application/dialogs/dialog-base";
import {IPromptDialogModel, PromptDialog} from "@application/dialogs/prompt-dialog/prompt-dialog";

export class DialogUtil {
    public static prompt(dialogService: IDialogService, options: IPromptDialogModel) {
        return DialogBase.toggle(dialogService, PromptDialog, options);
    }
}
