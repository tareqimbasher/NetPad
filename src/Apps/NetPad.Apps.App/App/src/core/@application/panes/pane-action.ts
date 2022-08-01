export class PaneAction {
    constructor(public text: string, public title: string, public execute: () => (void | Promise<void | unknown>)) {
    }
}
