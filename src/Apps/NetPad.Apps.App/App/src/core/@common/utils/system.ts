// import {shell} from "electron";

export class System {
    public static async openUrlInBrowser(url: string): Promise<void> {
        // await shell.openExternal(url);
        window.open(url, "_blank");
    }

    public static isRunningInElectron(): boolean {
        return navigator.userAgent.toLowerCase().indexOf(' electron/') > -1;
    }
}
