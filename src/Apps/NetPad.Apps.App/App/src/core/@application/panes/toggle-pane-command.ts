/**
 * Instructs app to toggle a specific pane.
 */
export class TogglePaneCommand {
    constructor(public readonly paneId: string) {
    }
}
