import {Dialog} from "@application/dialogs/dialog";

export interface IAlertDialogModel {
    title?: string;
    message: string;
}

export class AlertDialog extends Dialog<IAlertDialogModel> {
}
