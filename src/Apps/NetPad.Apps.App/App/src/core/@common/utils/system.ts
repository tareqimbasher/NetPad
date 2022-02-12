import {shell} from "electron";

export class System {
    public static async openUrlInBrowser(url: string): Promise<void> {
        await shell.openExternal(url);
    }
}
