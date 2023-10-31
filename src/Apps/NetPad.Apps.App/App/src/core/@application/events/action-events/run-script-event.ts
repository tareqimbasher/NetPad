/**
 * An action event instructing app to run a specific script.
 */
export class RunScriptEvent {
    constructor(public readonly scriptId?: string) {
    }
}
