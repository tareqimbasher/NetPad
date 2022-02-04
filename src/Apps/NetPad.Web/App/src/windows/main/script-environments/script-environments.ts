import {IScriptService, ISession} from "@domain";

export class ScriptEnvironments {
    constructor(
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService) {
    }

    public async binding() {
        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptService.create();
        }
    }
}
