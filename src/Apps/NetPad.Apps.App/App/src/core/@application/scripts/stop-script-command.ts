/**
 * Instructs app to stop a running script.
 */
export class StopScriptCommand {
    constructor(public readonly scriptId?: string) {
    }
}
