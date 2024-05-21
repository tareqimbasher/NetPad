export class ScriptCodeUpdatedEvent {

    constructor(public readonly scriptId: string, public readonly newCode: string) { }
}
