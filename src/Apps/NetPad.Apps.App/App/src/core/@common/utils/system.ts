export class System {
    public static async openUrlInBrowser(url: string): Promise<void> {
        if (this.isRunningInElectron()) {
            /* eslint-disable @typescript-eslint/no-var-requires */
            await require("electron").shell.openExternal(url);
            /* eslint-enable @typescript-eslint/no-var-requires */
        } else
            window.open(url, "_blank");
    }

    public static isRunningInElectron(): boolean {
        return navigator.userAgent.toLowerCase().indexOf(' electron/') > -1;
    }
}
