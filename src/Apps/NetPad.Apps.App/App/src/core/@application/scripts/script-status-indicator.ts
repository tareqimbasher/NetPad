import {ScriptEnvironment} from "@application";

/**
 * A script state shown as a status dot. `undefined` means idle, drawn as a hollow ring.
 */
export type ScriptStatusIndicator = "running" | "stopping" | "success" | "error";

/**
 * Maps a script environment to its status dot. A script that has not run this session is idle, not
 * successful, so "Ready" counts as success only once a run duration is recorded.
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
