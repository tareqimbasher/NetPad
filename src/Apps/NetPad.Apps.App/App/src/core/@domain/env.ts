import {System} from "@common";

export class Env {
    public static Environment: "DEV" | "PRD" = (process.env["ENVIRONMENT"] as ("DEV" | "PRD"));
    public static RemoteLoggingEnabled = process.env["REMOTE_LOGGING_ENABLED"]?.toLowerCase() === "true";

    public static get isDebug(): boolean {
        return Env.Environment === "DEV";
    }

    public static get isProduction(): boolean {
        return Env.Environment === "PRD";
    }

    public static isRunningInElectron(): boolean {
        return System.isRunningInElectron();
    }
}
