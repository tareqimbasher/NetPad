import {IDisposable, System} from "@common";
import {IMenuItem} from "./imenu-item";
import {
    ApiException,
    EnvironmentPropertyChangedEvent,
    IEventBus,
    IPaneManager,
    IScriptService,
    ISession,
    ISettingsService,
    IShortcutManager,
    IWindowService,
    RecentScriptsStore,
    ShortcutIds
} from "@application";
import {ITranslationService} from "@application/i18n/itranslation-service";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {AppUpdateDialog} from "@application/app/app-update-dialog/app-update-dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {IMainMenuService} from "./imain-menu-service";
import {WindowParams} from "@application/windows/window-params";
import {ShellType} from "@application/windows/shell-type";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";

export class MainMenuService implements IMainMenuService {
    private _items: IMenuItem[] = [];
    private readonly _onChangedCallbacks = new Set<() => void>();
    public readonly initialized: Promise<void>;

    constructor(
        @IScriptService private readonly scriptService: IScriptService,
        @ISettingsService private readonly settingsService: ISettingsService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ITextEditorService private readonly textEditorService: ITextEditorService,
        @IWindowService private readonly windowService: IWindowService,
        @IPaneManager private readonly paneManager: IPaneManager,
        @ISession private readonly session: ISession,
        @IEventBus eventBus: IEventBus,
        private readonly dialogUtil: DialogUtil,
        private readonly recentScriptsStore: RecentScriptsStore,
        @ITranslationService private readonly translation: ITranslationService
    ) {
        this._items = this.buildMenuItems();

        this.updateMenuItems();

        eventBus.subscribeToServer(EnvironmentPropertyChangedEvent, _ => this.updateMenuItems());

        this.initialized = this.recentScriptsStore.initialize();
        this.recentScriptsStore.onChanged(() => this.applyRecentMenu());
        this.applyRecentMenu();

        // Rebuild the menu after a language change (menu text is fixed at build time, so the
        // menu must be rebuilt to pick up the new language).
        this.translation.onLanguageChanged(() => {
            this._items = this.buildMenuItems();
            this.applyRecentMenu();
            this.fireChanged();
        });
    }

    private tr(key: string): string {
        return this.translation.t(key);
    }

    private buildMenuItems(): IMenuItem[] {
        return [
            {
                text: this.tr("menu.file"),
                menuItems: [
                    {
                        id: "file.new",
                        text: this.tr("menu.file.new"),
                        icon: "add-script-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.newDocument),
                    },
                    ...(WindowParams.shell === ShellType.Browser ? [] : [
                        {
                            id: "file.open",
                            text: this.tr("menu.file.openFile"),
                            shortcut: this.shortcutManager.getShortcut(ShortcutIds.openFile),
                        },
                        {
                            id: "file.openRecent",
                            text: this.tr("menu.file.openRecent"),
                            menuItems: []
                        },
                    ] as IMenuItem[]),
                    {
                        id: "file.goToScript",
                        text: this.tr("menu.file.goToScript"),
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.quickOpenDocument),
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "file.save",
                        text: this.tr("menu.file.save"),
                        icon: "save-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.saveDocument),
                    },
                    ...(WindowParams.shell === ShellType.Browser ? [] : [{
                        id: "file.saveAs",
                        text: this.tr("menu.file.saveAs"),
                        icon: "save-icon",
                        click: async () => {
                            const activeId = this.session.active?.script.id;
                            if (activeId) await this.scriptService.saveAs(activeId);
                        }
                    }] as IMenuItem[]),
                    {
                        id: "file.saveAll",
                        text: this.tr("menu.file.saveAll"),
                        icon: "save-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.saveAllDocuments),
                    },
                    {
                        id: "file.properties",
                        text: this.tr("menu.file.properties"),
                        icon: "properties-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.openDocumentProperties),
                    },
                    {
                        id: "file.close",
                        text: this.tr("menu.file.close"),
                        icon: "close-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.closeDocument),
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "file.settings",
                        text: this.tr("menu.file.settings"),
                        icon: "settings-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.openSettings),
                    },
                    ...(WindowParams.shell === ShellType.Browser ? [] : [{
                        id: "file.exit",
                        text: this.tr("menu.file.exit"),
                        click: async () => this.windowService.close()
                    }] as IMenuItem[])
                ]
            },
            {
                text: this.tr("menu.edit"),
                menuItems: [
                    {
                        id: "edit.undo",
                        text: this.tr("menu.edit.undo"),
                        icon: "undo-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "undo", null),
                        helpText: "Ctrl + Z"
                    },
                    {
                        id: "edit.redo",
                        text: this.tr("menu.edit.redo"),
                        icon: "redo-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "redo", null),
                        helpText: "Ctrl + Shift + Z"
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "edit.selectAll",
                        text: this.tr("menu.edit.selectAll"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.selectAll", null),
                        helpText: "Ctrl + A"
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "edit.find",
                        text: this.tr("menu.edit.find"),
                        icon: "search-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "actions.findWithSelection", null),
                        helpText: "Ctrl + F"
                    },
                    {
                        id: "edit.replace",
                        text: this.tr("menu.edit.replace"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.startFindReplaceAction", null),
                        helpText: "Ctrl + H"
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "edit.transform1",
                        text: this.tr("menu.edit.transformUpperLower"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "netpad.action.transformToUpperOrLowercase", null),
                        helpText: "Ctrl + Shift + Y"
                    },
                    {
                        id: "edit.transform2",
                        text: this.tr("menu.edit.transformUpper"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToUppercase", null)
                    },
                    {
                        id: "edit.transform3",
                        text: this.tr("menu.edit.transformLower"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToLowercase", null)
                    },
                    {
                        id: "edit.transform4",
                        text: this.tr("menu.edit.transformTitle"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToTitlecase", null)
                    },
                    {
                        id: "edit.transform5",
                        text: this.tr("menu.edit.transformKebab"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToKebabcase", null)
                    },
                    {
                        id: "edit.transform6",
                        text: this.tr("menu.edit.transformSnake"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToSnakecase", null)
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "edit.toggleLineComment",
                        text: this.tr("menu.edit.toggleLineComment"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.commentLine", null),
                        helpText: "Ctrl + /"
                    },
                    {
                        id: "edit.toggleBlockComment",
                        text: this.tr("menu.edit.toggleBlockComment"),
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.blockComment", null),
                        helpText: "Ctrl + Shift + A"
                    }
                ]
            },
            {
                text: this.tr("menu.view"),
                menuItems: [
                    {
                        id: "view.explorer",
                        text: this.tr("menu.view.explorer"),
                        icon: "explorer-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.openExplorer),
                    },
                    {
                        id: "view.output",
                        text: this.tr("menu.view.output"),
                        icon: "output-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.openOutput),
                    },
                    {
                        id: "view.code",
                        text: this.tr("menu.view.code"),
                        icon: "code-icon",
                        click: async () => {
                            const CodePane = (await import("../../../windows/main/panes")).CodePane;
                            this.paneManager.toggle(CodePane);
                        }
                    },
                    {
                        id: "view.namespaces",
                        text: this.tr("menu.view.namespaces"),
                        icon: "namespaces-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.openNamespaces),
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "view.reload",
                        text: this.tr("menu.view.reload"),
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.reloadWindow),
                    },
                    {
                        id: "view.toggleDeveloperTools",
                        text: this.tr("menu.view.toggleDeveloperTools"),
                        click: async () => this.windowService.toggleDeveloperTools(),
                        helpText: "Ctrl + Shift + I",
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "view.zoomIn",
                        text: this.tr("menu.view.zoomIn"),
                        icon: "zoom-in-icon",
                        // shortcut: this.shortcutManager.getShortcut("zoomIn"),
                        click: async () => this.windowService.zoomIn()
                    },
                    {
                        id: "view.zoomOut",
                        text: this.tr("menu.view.zoomOut"),
                        icon: "zoom-out-icon",
                        shortcut: this.shortcutManager.getShortcut(ShortcutIds.zoomOut),
                    },
                    {
                        id: "view.resetZoom",
                        text: this.tr("menu.view.resetZoom"),
                        helpText: "Ctrl + 0",
                        click: async () => this.windowService.resetZoom()
                    },
                    {
                        isDivider: true
                    },
                    {
                        id: "view.toggleFullScreen",
                        text: this.tr("menu.view.toggleFullScreen"),
                        click: async () => this.windowService.toggleFullScreen(),
                        helpText: "F11",
                    },
                ]
            },
            {
                text: this.tr("menu.tools"),
                menuItems: [
                    {
                        id: "tools.dependencyCheck",
                        text: this.tr("menu.tools.dependencyCheck"),
                        icon: "app-deps-check-icon",
                        click: async () => await this.dialogUtil.toggle(AppDependenciesCheckDialog)
                    },
                    {
                        id: "tools.stopRunningScripts",
                        text: this.tr("menu.tools.stopRunningScripts"),
                        hoverText: this.tr("menu.tools.stopRunningScripts.hover"),
                        icon: "stop-icon text-red",
                        click: async () => this.scriptService.stopAll(false),
                    },
                    {
                        id: "tools.stopScriptHosts",
                        text: this.tr("menu.tools.stopScriptHosts"),
                        hoverText: this.tr("menu.tools.stopScriptHosts.hover"),
                        icon: "stop-icon",
                        click: async () => this.scriptService.stopAll(true),
                    },
                ]
            },
            {
                text: this.tr("menu.help"),
                menuItems: [
                    {
                        id: "help.wiki",
                        text: this.tr("menu.help.wiki"),
                        icon: "wiki-icon",
                        click: async () => System.openUrlInBrowser("https://tareqimbasher.github.io/NetPad")
                    },
                    {
                        id: "help.github",
                        text: this.tr("menu.help.github"),
                        icon: "github-icon",
                        click: async () => System.openUrlInBrowser("https://github.com/tareqimbasher/NetPad")
                    },
                    {
                        id: "help.searchIssues",
                        text: this.tr("menu.help.searchIssues"),
                        icon: "github-icon",
                        click: async () => System.openUrlInBrowser("https://github.com/tareqimbasher/NetPad/issues")
                    },
                    {isDivider: true},
                    {
                        id: "help.checkForUpdates",
                        text: this.tr("menu.help.checkForUpdates"),
                        icon: "app-update-icon",
                        click: async () => await this.dialogUtil.toggle(AppUpdateDialog)
                    },
                    {
                        id: "help.about",
                        text: this.tr("menu.help.about"),
                        icon: "star-icon",
                        click: async () => await this.settingsService.openSettingsWindow("about")
                    },
                ]
            }
        ];
    }

    private applyRecentMenu() {
        const submenu = this.find(this._items, item => item.id === "file.openRecent");
        if (!submenu) return;

        const paths = this.recentScriptsStore.recentScripts;

        const newItems: IMenuItem[] = paths.map((path, ix) => ({
            id: `file.openRecent.${ix}`,
            text: path,
            hoverText: path,
            click: async () => {
                try {
                    await this.session.openByPath(path);
                } catch (err) {
                    if (err instanceof ApiException && err.status === 404) {
                        try {
                            await this.recentScriptsStore.remove(path);
                        } catch (removeErr) {
                            console.error("Failed to remove recent entry:", path, removeErr);
                        }
                    }
                }
            },
        }));

        if (newItems.length > 0) {
            newItems.push({isDivider: true});
            newItems.push({
                id: "file.openRecent.clear",
                text: this.tr("menu.file.openRecent.clear"),
                click: async () => {
                    try {
                        await this.recentScriptsStore.clear();
                    } catch (err) {
                        console.error("Failed to clear recent scripts:", err);
                    }
                }
            });
        }

        submenu.menuItems = newItems;
        submenu.disabled = paths.length === 0;

        this.fireChanged();
    }

    public get items(): ReadonlyArray<IMenuItem> {
        return this._items;
    }

    public onChanged(callback: () => void): IDisposable {
        this._onChangedCallbacks.add(callback);
        return {dispose: () => this._onChangedCallbacks.delete(callback)};
    }

    private fireChanged() {
        for (const callback of this._onChangedCallbacks) {
            try {
                callback();
            } catch (err) {
                console.error("A main menu onChanged callback threw:", err);
            }
        }
    }

    public async clickMenuItem(itemOrId: IMenuItem | string) {
        let menuItem: IMenuItem | undefined;

        if (typeof itemOrId === "object") {
            menuItem = itemOrId;
        } else {
            menuItem = this.find(this._items, item => item.id === itemOrId);
        }

        if (!menuItem) return;

        if (menuItem.click) {
            await menuItem.click();
        } else if (menuItem.shortcut) {
            await this.shortcutManager.executeShortcut(menuItem.shortcut);
        }
    }

    private filter(items: IMenuItem[], predicate: (item: IMenuItem) => boolean) {
        const results: IMenuItem[] = [];

        this.walkItems(items, item => {
            if (predicate(item)) results.push(item);
            return true;
        });

        return results;
    }

    private find(items: IMenuItem[], predicate: (item: IMenuItem) => boolean) {
        let result: IMenuItem | undefined;

        this.walkItems(items, item => {
            if (predicate(item)) {
                result = item;
                return false;
            }

            return true;
        });

        return result;
    }

    private walkItems(items: IMenuItem[], action: (item: IMenuItem) => boolean) {
        for (const item of items) {
            if (!action(item)) return;

            if (item.menuItems && item.menuItems.length) {
                this.walkItems(item.menuItems, action);
            }
        }
    }

    private updateMenuItems() {
        let anyScriptRunning = false;
        let anyScriptHostRunning = false;

        for (const environment of this.session.environments) {
            if (!anyScriptRunning && environment.status === "Running") {
                anyScriptRunning = true;
            }

            if (!anyScriptHostRunning && environment.isScriptHostRunning) {
                anyScriptHostRunning = true;
            }

            if (anyScriptRunning || anyScriptHostRunning) {
                break;
            }
        }

        let changed = false;

        let item = this.find(this._items, x => x.id === "tools.stopRunningScripts");
        if (item && item.disabled !== !anyScriptRunning) {
            item.disabled = !anyScriptRunning;
            changed = true;
        }

        item = this.find(this._items, x => x.id === "tools.stopScriptHosts");
        if (item && item.disabled !== !anyScriptHostRunning) {
            item.disabled = !anyScriptHostRunning;
            changed = true;
        }

        if (changed) {
            this.fireChanged();
        }
    }
}
