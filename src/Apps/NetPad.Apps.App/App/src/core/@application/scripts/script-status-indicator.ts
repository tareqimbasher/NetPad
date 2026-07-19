import {ScriptEnvironment} from "@application";

/**
 * The state vocabulary rendered as a dot wherever script state is surfaced.
 * `undefined` means idle, drawn as a hollow ring.
 */
export type ScriptStatusIndicator = "running" | "stopping" | "success" | "error";

/**
 * Maps a script environment onto the dot vocabulary. A script that has never run this session
 * is idle, not successful, which is why "Ready" depends on a recorded run duration.
 */
export function resolveScriptStatusIndicator(environment: ScriptEnvironment | undefined | null): ScriptStatusIndicator | undefined {
    if (!environment) return undefined;

    switch (environment.status) {
        case "Running":
            return "running";
        case "Stopping":
            return "stopping";
        case "Error":
            return "error";
        case "Ready":
            return environment.runDurationMilliseconds != null ? "success" : undefined;
        default:
            return undefined;
    }
}

/** The tooltip that accompanies a status dot. */
export function scriptStatusIndicatorTitle(indicator: ScriptStatusIndicator | undefined): string {
    switch (indicator) {
        case "running":
            return "Running...";
        case "stopping":
            return "Stopping...";
        case "success":
            return "Script ran successfully";
        case "error":
            return "Error occurred";
        default:
            return "Idle";
    }
}
