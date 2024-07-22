/**
 * Instructs app to run a specific script.
 */
export class RunScriptCommand {
    constructor(public readonly scriptId?: string) {
    }
}
