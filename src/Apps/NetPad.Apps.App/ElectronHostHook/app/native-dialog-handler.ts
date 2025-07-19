import {ipcMain, dialog} from "electron";
import {electronConstants} from "../electron-shared";

// https://www.electronjs.org/docs/latest/api/dialog#dialogshowopendialogsyncwindow-options
type ElectronOpenDialogProperty = 'openFile' | 'openDirectory' | 'multiSelections' | 'showHiddenFiles'
    | 'createDirectory' | 'promptToCreate' | 'noResolveAliases' | 'treatPackageAsDirectory' | 'dontAddToRecent';

export class NativeDialogHandler {
    public static init() {
        ipcMain.handle(electronConstants.ipcEventNames.openFileSelectorDialog, async (ev, ...args) => {
            if (!args || args.length === 0) {
                console.error("No open file selector dialog arguments passed.");
                return null;
            }

            const options = args[0];
            const properties: ElectronOpenDialogProperty[] = [
                "createDirectory",
            ];

            if (options.directory) {
                properties.push("openDirectory");
            } else {
                properties.push("openFile");
            }

            if (options.multiple) {
                properties.push("multiSelections");
            }

            const result = await dialog.showOpenDialog({
                title: options.title,
                filters: options.filters,
                defaultPath: options.defaultPath,
                properties: properties
            });

            if (result.canceled) {
                return null;
            }

            return result.filePaths;
        });
    }
}
