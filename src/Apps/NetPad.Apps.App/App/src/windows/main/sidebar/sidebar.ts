import {
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    ScriptDirectoryChanged,
    ScriptSummary,
    Settings
} from "@domain";
import Split from "split.js";
import {Util} from "@common";

export class Sidebar {
    private readonly rootScriptFolder: SidebarScriptFolder;

    constructor(@ISession readonly session: ISession,
                @IScriptService readonly scriptService: IScriptService,
                @IAppService readonly appService: IAppService,
                @IEventBus readonly eventBus: IEventBus,
                readonly settings: Settings) {
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

        this.eventBus.subscribeToServer(ScriptDirectoryChanged, msg => {
            this.loadScripts(msg.scripts);
        });
    }

    private loadScripts(summaries: ScriptSummary[]) {
        const expandedFolders = new Set<string>();
        this.recurseFolders(this.rootScriptFolder, folder => {
            if (folder.expanded)
                expandedFolders.add(folder.path);
        });

        const root = this.rootScriptFolder.clone();

        const scriptsDirPath = Util.trimEnd(
            this.settings.scriptsDirectoryPath.replaceAll("\\", "/"), "/");

        for (const summary of summaries) {
            let path = summary.path.replaceAll("\\", "/");

            if (path.startsWith(scriptsDirPath)) {
                path = "/" + Util.trim(path.substring(scriptsDirPath.length), "/");
            }

            const folderParts = path
                .split("/")
                .filter(x => !!x)
                .slice(0, -1);

            const folder = this.getFolder(root, folderParts);
            folder.scripts.push(summary);
        }

        this.recurseFolders(root, folder => {
            if (expandedFolders.has(folder.path)) {
                folder.expanded = true;
            }
        });

        this.rootScriptFolder.scripts = root.scripts;
        this.rootScriptFolder.folders = root.folders;
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
}

class SidebarScriptFolder {
    constructor(public name: string, public path: string, public parent: SidebarScriptFolder | null) {
    }

    public expanded = false;
    public folders: SidebarScriptFolder[] = [];
    public scripts: ScriptSummary[] = [];

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
