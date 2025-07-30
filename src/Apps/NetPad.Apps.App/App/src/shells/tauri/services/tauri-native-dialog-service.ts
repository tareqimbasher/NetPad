import {open} from '@tauri-apps/plugin-dialog';
import {FileSelectorDialogOptions, INativeDialogService} from "@application/dialogs/inative-dialog-service";

export class TauriNativeDialogService implements INativeDialogService {
    public async showFileSelectorDialog(options: FileSelectorDialogOptions): Promise<string[] | null> {
        const selected = await open({
            title: options.title,
            filters: options.filters,
            defaultPath: options.defaultPath,
            multiple: options.multiple,
            directory: options.directory,
        });

        if (Array.isArray(selected)) {
            return selected as string[];
        } else if (!selected) {
            return null;
        } else {
            return [selected];
        }
    }
}
