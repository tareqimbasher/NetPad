import {shell} from "electron";
import {ISystemService} from "@application/system/isystem-service";

export class ElectronSystemService implements ISystemService {
    public openUrlInBrowser(url: string): void {
        const _ = shell.openExternal(url);
    }
}
