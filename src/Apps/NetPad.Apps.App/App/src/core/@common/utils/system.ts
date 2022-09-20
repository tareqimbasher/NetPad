import {Env} from "@application";

export class System {
    /**
     * Opens a URL in system-configured default browser.
     * @param url The URL to open.
     */
    public static async openUrlInBrowser(url: string): Promise<void> {
        if (Env.isRunningInElectron()) {
            /* eslint-disable @typescript-eslint/no-var-requires */
            await require("electron").shell.openExternal(url);
            /* eslint-enable @typescript-eslint/no-var-requires */
        } else
            window.open(url, "_blank");
    }
}
