export class Env {
    public static Environment: "DEV" | "PRD" = (process.env["ENVIRONMENT"] as ("DEV" | "PRD"));

    public static get isDebug(): boolean {
        return Env.Environment === "DEV";
    }

    public static get isProduction(): boolean {
        return Env.Environment === "PRD";
    }
}
