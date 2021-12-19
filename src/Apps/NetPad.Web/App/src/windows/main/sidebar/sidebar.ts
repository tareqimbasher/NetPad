import {IAppService, IScriptService, ISession, ScriptSummary} from "@domain";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@ISession readonly session: ISession,
                @IScriptService readonly scriptService: IScriptService,
                @IAppService readonly appService: IAppService) {
    }

    public async attached() {
        this.scripts = await this.scriptService.getScripts();
    }

    public async openScriptsFolder() {
        await this.appService.openScriptsFolder();
    }
}
