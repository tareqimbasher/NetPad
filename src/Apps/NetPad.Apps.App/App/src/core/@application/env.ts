export class Env {
    public static Environment: "DEV" | "PRD" = (process.env.ENVIRONMENT as ("DEV" | "PRD"));

    /**
     * Determines if app is running in Electron.
     */
    public static isRunningInElectron(): boolean {
        return navigator.userAgent.toLowerCase().indexOf(' electron/') > -1;
    }
}