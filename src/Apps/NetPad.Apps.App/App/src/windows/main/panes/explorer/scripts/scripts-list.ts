import {watch} from "@aurelia/runtime-html";
import {
    IAppService,
    IDataConnectionService,
    IEventBus,
    IScriptService,
    ISession,
    ScriptDirectoryChangedEvent,
    ScriptEnvironment,
    ScriptSummary,
    Settings,
    ViewModelBase
} from "@application";
import {Util} from "@common";
import {ILogger} from "aurelia";
import {ScriptFolderViewModel} from "./script-folder-view-model";
import {ScriptViewModel} from "./script-view-model";

export class ScriptsList extends ViewModelBase {
    private readonly rootScriptFolder: ScriptFolderViewModel;
    private scriptsMap: Map<string, ScriptViewModel>;

    constructor(@ISession private readonly session: ISession,
                @IScriptService private readonly scriptService: IScriptService,
                @IAppService private readonly appService: IAppService,
                @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
                @IEventBus private readonly eventBus: IEventBus,
                private readonly settings: Settings,
                @ILogger logger: ILogger) {

        super(logger);
        this.scriptsMap = new Map<string, ScriptViewModel>();
        this.rootScriptFolder = new ScriptFolderViewModel("/", "/", null);
        this.rootScriptFolder.expanded = true;
    }

    public async attached() {
        try {
            this.loadScripts(await this.scriptService.getScripts());
        } catch (ex) {
            this.logger.error("Error loading scripts", ex);
        }

        this.eventBus.subscribeToServer(ScriptDirectoryChangedEvent, msg => {
            this.loadScripts(msg.scripts);
        });
    }

    public async openScriptsFolder(folder: ScriptFolderViewModel) {
        await this.appService.openScriptsFolder(folder.path);
    }

    public expandAllFolders(folder: ScriptFolderViewModel) {
        folder.expanded = true;
        folder.folders.forEach(f => this.expandAllFolders(f));
    }

    public collapseAllFolders(folder: ScriptFolderViewModel) {
        folder.expanded = false;
        folder.folders.forEach(f => this.collapseAllFolders(f));
    }

    private loadScripts(summaries: ScriptSummary[]) {
        const scripts = summaries.map(s => new ScriptViewModel(s));

        const expandedFolders = new Set<string>();
        this.recurseFolders(this.rootScriptFolder, folder => {
            if (folder.expanded)
                expandedFolders.add(folder.path);
        });

        const root = this.rootScriptFolder.clone();

        const scriptsDirPath: string = Util.trimEnd(
            this.settings.scriptsDirectoryPath.replaceAll("\\", "/"), "/");

        for (const script of scripts) {
            let path = script.path.replaceAll("\\", "/");

            if (path.startsWith(scriptsDirPath)) {
                path = "/" + Util.trim(path.substring(scriptsDirPath.length), "/");
            }

            const folderParts = path
                .split("/")
                .filter(x => !!x)
                .slice(0, -1);

            const folder = this.getFolder(root, folderParts);
            folder.scripts.push(script);
        }

        this.recurseFolders(root, folder => {
            if (expandedFolders.has(folder.path)) {
                folder.expanded = true;
            }
        });

        this.rootScriptFolder.scripts = root.scripts;
        this.rootScriptFolder.folders = root.folders;

        this.scriptsMap = new Map<string, ScriptViewModel>(scripts.map(s => [s.id, s]));
        this.hydrateScriptMarkers();
    }

    private getFolder(parent: ScriptFolderViewModel, folderPathParts: string[]): ScriptFolderViewModel {
        let result = parent;

        for (const folderName of folderPathParts) {
            let folder = result.folders.find(f => f.name === folderName);
            if (!folder) {
                folder = new ScriptFolderViewModel(folderName, folderPathParts.join("/"), parent);
                result.folders.push(folder);
            }
            result = folder;
        }

        return result;
    }

    private recurseFolders(folder: ScriptFolderViewModel, func: (f: ScriptFolderViewModel) => void) {
        func(folder);

        for (const subFolder of folder.folders) {
            this.recurseFolders(subFolder, func);
        }
    }

    @watch<ScriptsList>(vm => vm.session.environments.length)
    private hydrateScriptMarkers() {
        const openEnvs = new Map<string, ScriptEnvironment>(this.session.environments.map(e => [e.script.id, e]));
        for (const script of this.scriptsMap.values()) {
            script.environment = openEnvs.get(script.id);
        }
    }

    @watch<ScriptsList>(vm => vm.session.active)
    private hydrateActiveScript() {
        const activeScriptId = this.session.active?.script.id;

        for (const script of this.scriptsMap.values()) {
            script.isActive = false;
        }

        if (activeScriptId) {
            const script = this.scriptsMap.get(activeScriptId);
            if (script) script.isActive = true;
        }
    }
}
