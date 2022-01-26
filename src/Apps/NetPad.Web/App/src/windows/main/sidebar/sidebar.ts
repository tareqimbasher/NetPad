import {IAppService, IScriptService, ISession, ScriptSummary} from "@domain";
import Split from "split.js";

export class Sidebar {
    private scripts: ScriptSummary[] = [];

    constructor(@ISession readonly session: ISession,
                @IScriptService readonly scriptService: IScriptService,
                @IAppService readonly appService: IAppService) {
    }

    public async attached() {
        this.scripts = await this.scriptService.getScripts();

        Split(["#connection-list", "#script-list"], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [35, 65],
            minSize: [100, 100],
        });
    }

    public async openScriptsFolder() {
        await this.appService.openScriptsFolder();
    }

    public async addConnection() {
        alert("Not implemented yet.");
    }
}
