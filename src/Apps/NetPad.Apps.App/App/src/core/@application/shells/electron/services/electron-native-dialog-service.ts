import {ipcRenderer} from "electron";
import {FileSelectorDialogOptions, INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {electronConstants} from "@application/shells/electron/electron-shared";

export class ElectronNativeDialogService implements INativeDialogService {
    public async showFileSelectorDialog(options: FileSelectorDialogOptions): Promise<string[] | null> {
        return await ipcRenderer.invoke(electronConstants.ipcEventNames.openFileSelectorDialog, options);
    }
}
