import {FileSelectorDialogOptions, INativeDialogService} from "@application/dialogs/inative-dialog-service";

export class BrowserNativeDialogService implements INativeDialogService {
    public async showFileSelectorDialog(options: FileSelectorDialogOptions): Promise<string[] | null> {
        console.warn("showFileSelectorDialog is not supported in browser");
        return Promise.resolve(null);
    }
}
