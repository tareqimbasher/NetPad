import {IScriptManager, ISession, ScriptSummary} from "@domain";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@ISession readonly session: ISession, @IScriptManager readonly scriptManager: IScriptManager) {
    }

    public async attached() {
        this.scripts = await this.scriptManager.getScripts();
    }
}
