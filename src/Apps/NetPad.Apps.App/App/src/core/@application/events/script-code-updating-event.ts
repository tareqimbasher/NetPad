export class ScriptCodeUpdatingEvent {

    constructor(public readonly scriptId: string, public readonly newCode: string) { }
}
