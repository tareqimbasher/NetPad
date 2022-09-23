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
    Settings
} from "@domain";
import {Util} from "@common";
import {ViewModelBase} from "@application";
import {ILogger} from "aurelia";
import {SidebarScriptFolder} from "./sidebar-script-folder";
import {SidebarScript} from "./sidebar-script";

export class ScriptsList extends ViewModelBase {
    private readonly rootScriptFolder: SidebarScriptFolder;
    private scriptsMap: Map<string, SidebarScript>;

    constructor(@ISession private readonly  session: ISession,
                @IScriptService private readonly  scriptService: IScriptService,
                @IAppService private readonly  appService: IAppService,
                @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
                @IEventBus private readonly  eventBus: IEventBus,
                private readonly  settings: Settings,
                @ILogger logger: ILogger) {

        super(logger);
        this.scriptsMap = new Map<string, SidebarScript>();
        this.rootScriptFolder = new SidebarScriptFolder("/", "/", null);
        this.rootScriptFolder.expanded = true;
    }

    public async attached() {
        try {
            this.loadScripts(await this.scriptService.getScripts());
        }
        catch (ex) {
            this.logger.error("Error loading scripts", ex);
        }

        this.eventBus.subscribeToServer(ScriptDirectoryChangedEvent, msg => {
            this.loadScripts(msg.scripts);
        });
    }

    public async openScriptsFolder(folder: SidebarScriptFolder) {
        await this.appService.openScriptsFolder(folder.path);
    }

    public expandAllFolders(folder: SidebarScriptFolder) {
        folder.expanded = true;
        folder.folders.forEach(f => this.expandAllFolders(f));
    }

    public collapseAllFolders(folder: SidebarScriptFolder) {
        folder.expanded = false;
        folder.folders.forEach(f => this.collapseAllFolders(f));
    }

    private loadScripts(summaries: ScriptSummary[]) {
        const scripts = summaries.map(s => new SidebarScript(s));

        const expandedFolders = new Set<string>();
        this.recurseFolders(this.rootScriptFolder, folder => {
            if (folder.expanded)
                expandedFolders.add(folder.path);
        });

        const root = this.rootScriptFolder.clone();

        const scriptsDirPath = Util.trimEnd(
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

        this.scriptsMap = new Map<string, SidebarScript>(scripts.map(s => [s.id, s]));
        this.hydrateScriptMarkers();
    }

    private getFolder(parent: SidebarScriptFolder, folderPathParts: string[]): SidebarScriptFolder {
        let result = parent;

        for (const folderName of folderPathParts) {
            let folder = result.folders.find(f => f.name === folderName);
            if (!folder) {
                folder = new SidebarScriptFolder(folderName, folderPathParts.join("/"), parent);
                result.folders.push(folder);
            }
            result = folder;
        }

        return result;
    }

    private recurseFolders(folder: SidebarScriptFolder, func: (f: SidebarScriptFolder) => void) {
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
