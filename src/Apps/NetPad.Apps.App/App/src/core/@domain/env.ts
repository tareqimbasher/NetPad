import {System} from "@common";

export class Env {
    public static Environment: "DEV" | "PRD" = (process.env.ENVIRONMENT as ("DEV" | "PRD"));
    public static RemoteLoggingEnabled = !!process.env.REMOTE_LOGGING_ENABLED && process.env.REMOTE_LOGGING_ENABLED.toLowerCase() === "true";

    /**
     * Determines if app is running in Electron.
     */
    public static isRunningInElectron(): boolean {
        return System.isRunningInElectron();
    }
}
