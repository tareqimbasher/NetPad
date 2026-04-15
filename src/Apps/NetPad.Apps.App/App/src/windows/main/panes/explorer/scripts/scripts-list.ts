import {watch} from "@aurelia/runtime-html";
import {
    ContextMenuOptions,
    IAppService,
    IDataConnectionService,
    IEventBus,
    IScriptService,
    ISession,
    RunOptions,
    ScriptDirectoryChangedEvent,
    ScriptEnvironment,
    ScriptSummary,
    Settings,
    ViewModelBase
} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {Util} from "@common";
import {ILogger} from "aurelia";
import {ScriptFolderViewModel} from "./script-folder-view-model";
import {ScriptViewModel} from "./script-view-model";

export class ScriptsList extends ViewModelBase {
    private readonly rootScriptFolder: ScriptFolderViewModel;
    private scriptsMap: Map<string, ScriptViewModel>;
    public scriptContextMenuOptions: ContextMenuOptions;
    public folderContextMenuOptions: ContextMenuOptions;

    constructor(private readonly element: HTMLElement,
                @ISession private readonly session: ISession,
                @IScriptService private readonly scriptService: IScriptService,
                @IAppService private readonly appService: IAppService,
                @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
                private readonly dialogUtil: DialogUtil,
                @IEventBus private readonly eventBus: IEventBus,
                private readonly settings: Settings,
                @ILogger logger: ILogger) {

        super(logger);
        this.scriptsMap = new Map<string, ScriptViewModel>();
        this.rootScriptFolder = new ScriptFolderViewModel("/", "/", null);
        this.rootScriptFolder.expanded = true;
    }

    public binding() {
        this.scriptContextMenuOptions = new ContextMenuOptions(".list-group-item.script", [
            {
                text: "Open",
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.session.openByPath(script.path);
                }
            },
            {
                icon: "run-icon",
                text: "Run",
                show: (target) => {
                    const script = this.getScriptFromElement(target);
                    if (!script?.environment) return false;
                    const status = script.environment.status;
                    return status !== "Running" && status !== "Stopping";
                },
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.scriptService.run(script.id, new RunOptions());
                }
            },
            {
                icon: "stop-icon text-red",
                text: "Stop",
                show: (target) => {
                    const script = this.getScriptFromElement(target);
                    return script?.environment?.status === "Running";
                },
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.scriptService.stop(script.id, undefined);
                }
            },
            {
                icon: "rename-icon",
                text: "Rename",
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.scriptService.openRenamePrompt(script);
                }
            },
            {
                icon: "duplicate-icon",
                text: "Duplicate",
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.scriptService.duplicate(script.id);
                }
            },
            {
                isDivider: true
            },
            {
                icon: "open-folder-icon",
                text: "Open Containing Folder",
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (script) await this.appService.openFolderContainingScript(script.path);
                }
            },
            {
                isDivider: true
            },
            {
                icon: "delete-icon text-red",
                text: "Delete",
                onSelected: async (target) => {
                    const script = this.getScriptFromElement(target);
                    if (!script) return;

                    const confirmation = await this.dialogUtil.ask({
                        message: `Delete '${script.name}'? This cannot be undone.`
                    });

                    if (confirmation.value === "OK") {
                        await this.scriptService.delete(script.id);
                    }
                }
            }
        ]);

        this.folderContextMenuOptions = new ContextMenuOptions(".list-group-item.script-folder", [
            {
                icon: "script-folder-open-icon",
                text: "Open in File Manager",
                onSelected: async (target) => {
                    const folder = this.getFolderFromElement(target);
                    if (folder) await this.appService.openScriptsFolder(folder.path);
                }
            },
            {
                isDivider: true
            },
            {
                icon: "delete-icon text-red",
                text: "Delete",
                show: (target) => {
                    const folder = this.getFolderFromElement(target);
                    return folder != null && folder.path !== "/";
                },
                onSelected: async (target) => {
                    const folder = this.getFolderFromElement(target);
                    if (!folder) {
                        return;
                    }

                    const confirmation = await this.dialogUtil.ask({
                        message: `Delete folder '${folder.name}' and the ${folder.containingScriptCount} scripts it contains? This cannot be undone.`
                    });

                    if (confirmation.value === "OK") {
                        await this.scriptService.deleteFolder(folder.path);
                    }
                }
            }
        ]);
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

        const expandedFolders = this.rootScriptFolder.findFolders(f => f.expanded);

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

        this.rootScriptFolder.scripts = root.scripts;
        this.rootScriptFolder.folders = root.folders;
        this.rootScriptFolder.updateStats(new Set(expandedFolders.map(f => f.path)));

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

    private getScriptFromElement(element: Element): ScriptViewModel | undefined {
        const id = this.getElementAttribute(element, "data-script-id");
        return id ? this.scriptsMap.get(id) : undefined;
    }

    private getFolderFromElement(element: Element): ScriptFolderViewModel | undefined {
        const path = this.getElementAttribute(element, "data-folder-path");
        if (!path) {
            return undefined;
        }

        if (this.rootScriptFolder.path === path) {
            return this.rootScriptFolder;
        }

        return this.rootScriptFolder.findFolder(folder => folder.path === path);
    }

    private getElementAttribute(element: Element, attributeName: string): string | null {
        let el: Element | null = element;

        do {
            const value = el.getAttribute(attributeName);
            if (value) {
                return value;
            }
            el = el.parentElement === this.element.parentElement ? null : el.parentElement;
        } while (el);

        return null;
    }
}
