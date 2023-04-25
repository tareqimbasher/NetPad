export class PaneAction {
    constructor(public text: string, public hoverText: string, public click: () => (void | Promise<void | unknown>)) {
    }
}
