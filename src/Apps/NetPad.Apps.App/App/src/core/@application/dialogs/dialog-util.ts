import {Constructable} from "aurelia";
import {DialogCloseResult, IDialogService} from "@aurelia/dialog";
import {Dialog} from "./dialog";
import {OpenDialogs} from "./open-dialogs";
import {IPromptDialogModel, PromptDialog} from "../app/prompt-dialog/prompt-dialog";
import {AskDialog, IAskDialogModel} from "../app/ask-dialog/ask-dialog";
import {AlertDialog, IAlertDialogModel} from "../app/alert-dialog/alert-dialog";

export class DialogUtil {
    constructor(@IDialogService private readonly dialogService: IDialogService) {
    }

    public async alert(options: IAlertDialogModel): Promise<DialogCloseResult> {
        const openResult = await this.open(AlertDialog, options);

        if (!openResult) {
            throw new Error("Error opening alert with message: " + options.message)
        }

        return openResult;
    }

    public async ask(options: IAskDialogModel): Promise<DialogCloseResult> {
        const openResult = await this.open(AskDialog, options);

        if (!openResult) {
            throw new Error("Error opening ask with message: " + options.message)
        }

        return openResult;
    }

    public async prompt(options: IPromptDialogModel): Promise<DialogCloseResult> {
        const openResult = await this.open(PromptDialog, options);

        if (!openResult) {
            throw new Error("Error opening prompt with message: " + options.message)
        }

        return openResult;
    }

    /**
     * Opens a dialog (a class that extends Dialog<>).
     * @param dialogComponent The dialog type to open.
     * @param input Input object to be passed to dialog when activated.
     * @param allowMultiple By default only one instance of a particular dialog type is opened, if this is true,
     * multiple dialogs of this dialog type will be allowed to be opened simultaneously.
     */
    public async open<TDialog extends typeof Dialog<TInput> | Constructable, TInput>(
        dialogComponent: TDialog,
        input?: TDialog extends typeof Dialog<infer U> ? U : unknown
    ): Promise<DialogCloseResult | undefined> {
        const key = dialogComponent.name;

        let openResult = OpenDialogs.get(key);

        if (openResult) {
            return;
        }

        openResult = await this.dialogService.open({
            component: () => dialogComponent,
            model: input
        });

        OpenDialogs.set(key, openResult);

        openResult.dialog.closed.then(result => {
            OpenDialogs.delete(key);
        });

        return openResult.dialog.closed;
    }

    /**
     * Closes a dialog if it is currently opened.
     * @param dialogComponent The dialog type to close.
     * */
    public async close<TDialog extends typeof Dialog<TInput> | Constructable, TInput>(dialogComponent: TDialog) {
        const key = dialogComponent.name;

        const openResult = OpenDialogs.get(key);

        if (!openResult) {
            return;
        }

        OpenDialogs.delete(key);

        return openResult.dialog.cancel();
    }

    public async closeAll(): Promise<void> {
        await this.dialogService.closeAll();
        OpenDialogs.clear();
    }

    public async toggle<TDialog extends typeof Dialog<TInput> | Constructable, TInput>(
        dialogComponent: TDialog,
        input?: TDialog extends typeof Dialog<infer U> ? U : unknown
    ): Promise<void> {
        const key = dialogComponent.name;

        const openResult = OpenDialogs.get(key);

        if (openResult) {
            await this.close(dialogComponent);
        } else {
            await this.open(dialogComponent, input);
        }
    }
}
