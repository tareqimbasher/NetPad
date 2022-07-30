import {watch} from "@aurelia/runtime-html";
import {
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    ScriptDirectoryChangedEvent, ScriptEnvironment,
    ScriptSummary,
    Settings
} from "@domain";
import Split from "split.js";
import {Util} from "@common";

export class Sidebar {
    private readonly rootScriptFolder: SidebarScriptFolder;
    private scriptsMap: Map<string, SidebarScript>;

    constructor(@ISession readonly session: ISession,
                @IScriptService readonly scriptService: IScriptService,
                @IAppService readonly appService: IAppService,
                @IEventBus readonly eventBus: IEventBus,
                readonly settings: Settings) {

        this.scriptsMap = new Map<string, SidebarScript>();
        this.rootScriptFolder = new SidebarScriptFolder("/", "/", null);
        this.rootScriptFolder.expanded = true;
    }

    public async attached() {
        this.loadScripts(await this.scriptService.getScripts());

        Split(["#connection-list", "#script-list"], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [35, 65],
            minSize: [100, 100],
        });

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

    public async addConnection() {
        alert("Adding connections is not implemented yet.");
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

    @watch<Sidebar>(vm => vm.session.environments.length)
    private hydrateScriptMarkers() {
        const openEnvs = new Map<string, ScriptEnvironment>(this.session.environments.map(e => [e.script.id, e]));
        for (const script of this.scriptsMap.values()) {
            script.environment = openEnvs.get(script.id);
        }
    }

    @watch<Sidebar>(vm => vm.session.active)
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

class SidebarScriptFolder {
    constructor(public name: string, public path: string, public parent: SidebarScriptFolder | null) {
    }

    public expanded = false;
    public folders: SidebarScriptFolder[] = [];
    public scripts: SidebarScript[] = [];

    public clone(deep = false): SidebarScriptFolder {
        const clone = new SidebarScriptFolder(this.name, this.path, this.parent);

        clone.expanded = this.expanded;

        if (deep) {
            for (const folder of this.folders) {
                clone.folders.push(folder.clone(deep));
            }
            for (const script of this.scripts) {
                clone.scripts.push(script);
            }
        }

        return clone;
    }
}

class SidebarScript extends ScriptSummary {
    constructor(summary: ScriptSummary) {
        super(summary);
    }

    public environment?: ScriptEnvironment;
    public isActive: boolean;

    public get cssClasses() : string {
        let classes = this.isActive ? 'is-active' : "";

        if (this.environment) {
            classes += " is-open";
            if (this.environment.script.isDirty) classes += " is-dirty";
            if (this.environment.status === "Ready") classes += " is-ready";
            else if (this.environment.status === "Running") classes += " is-running";
            else if (this.environment.status === "Error") classes += " is-error";
        }

        return classes;
    }
}
