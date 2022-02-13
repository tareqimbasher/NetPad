export class System {
    public static async openUrlInBrowser(url: string): Promise<void> {
        if (this.isRunningInElectron()) {
            await require("electron").shell.openExternal(url);
        } else
            window.open(url, "_blank");
    }

    public static isRunningInElectron(): boolean {
        return navigator.userAgent.toLowerCase().indexOf(' electron/') > -1;
    }
}
