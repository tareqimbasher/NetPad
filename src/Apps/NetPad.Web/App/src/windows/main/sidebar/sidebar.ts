import {IScriptService, ISession, ScriptSummary} from "@domain";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@ISession readonly session: ISession, @IScriptService readonly scriptService: IScriptService) {
    }

    public async attached() {
        this.scripts = await this.scriptService.getScripts();
    }
}
