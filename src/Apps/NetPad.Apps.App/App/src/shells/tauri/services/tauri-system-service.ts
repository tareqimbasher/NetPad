import {openUrl} from "@tauri-apps/plugin-opener";
import {ISystemService} from "@application/system/isystem-service";

export class TauriSystemService implements ISystemService {
    public openUrlInBrowser(url: string): void {
        const _ = openUrl(url);
    }
}
