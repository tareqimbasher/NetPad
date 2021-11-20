import {IScriptManager, ScriptSummary} from "@domain";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@IScriptManager readonly scriptManager: IScriptManager) {
    }

    public async attached() {
        this.scripts = await this.scriptManager.getScripts();
    }
}
