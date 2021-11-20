import {IScriptRepository, ScriptSummary} from "@domain";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@IScriptRepository readonly scriptRepository: IScriptRepository) {
    }

    public async attached() {
        this.scripts = await this.scriptRepository.getScripts();
    }
}
