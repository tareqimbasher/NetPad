import {IDisposable} from "@common";
import {IMenuItem} from "./imenu-item";
import {
    ApiException,
    CommandIds,
    EnvironmentPropertyChangedEvent,
    ICommandRegistry,
    IEventBus,
    IKeybindingManager,
    ISession,
    RecentScriptsStore
} from "@application";
import {MonacoEditorUtil} from "@application/editor/monaco/monaco-editor-util";
import {IMainMenuService} from "./imain-menu-service";
import {WindowParams} from "@application/windowing/window-params";
import {ShellType} from "@application/windowing/shell-type";

export class MainMenuService implements IMainMenuService {
    private readonly _items: IMenuItem[] = [];
    private readonly _onChangedCallbacks = new Set<() => void>();
    public readonly initialized: Promise<void>;

    constructor(
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
        @IKeybindingManager private readonly keybindingManager: IKeybindingManager,
        @ISession private readonly session: ISession,
        @IEventBus eventBus: IEventBus,
        private readonly recentScriptsStore: RecentScriptsStore
    ) {
        this._items = [
            {
                text: "File",
                menuItems: [
                    {commandId: CommandIds.newScript},
                    {text: "Open File...", commandId: CommandIds.openFile},
                    ...(WindowParams.shell === ShellType.Browser ? [] : [{
                        id: "file.openRecent",
                        text: "Open Recent",
                        menuItems: []
                    }] as IMenuItem[]),
                    {commandId: CommandIds.goToScript},
                    {isDivider: true},
                    {commandId: CommandIds.saveScript},
                    {text: "Save As...", commandId: CommandIds.saveScriptAs},
                    {commandId: CommandIds.saveAllScripts},
                    {text: "Properties", commandId: CommandIds.openScriptProperties},
                    {commandId: CommandIds.closeScript},
                    {isDivider: true},
                    {commandId: CommandIds.openSettings},
                    {commandId: CommandIds.exit},
                ]
            },
            {
                text: "Edit",
                menuItems: [
                    {commandId: CommandIds.undo},
                    {commandId: CommandIds.redo},
                    {isDivider: true},
                    {commandId: CommandIds.selectAll},
                    {isDivider: true},
                    {commandId: CommandIds.find},
                    {commandId: CommandIds.replace},
                    {isDivider: true},
                    {commandId: CommandIds.transformToUpperOrLowerCase},
                    {commandId: CommandIds.transformToUpperCase},
                    {commandId: CommandIds.transformToLowerCase},
                    {commandId: CommandIds.transformToTitleCase},
                    {commandId: CommandIds.transformToKebabCase},
                    {commandId: CommandIds.transformToSnakeCase},
                    {isDivider: true},
                    {commandId: CommandIds.toggleLineComment},
                    {commandId: CommandIds.toggleBlockComment},
                ]
            },
            {
                text: "View",
                menuItems: [
                    {commandId: CommandIds.toggleExplorerPane},
                    {commandId: CommandIds.toggleOutputPane},
                    {commandId: CommandIds.toggleCodePane},
                    {commandId: CommandIds.toggleNamespacesPane},
                    {isDivider: true},
                    {commandId: CommandIds.reloadWindow},
                    {commandId: CommandIds.toggleDeveloperTools},
                    {isDivider: true},
                    {commandId: CommandIds.zoomIn},
                    {commandId: CommandIds.zoomOut},
                    {commandId: CommandIds.zoomReset},
                    {isDivider: true},
                    {commandId: CommandIds.toggleFullScreen},
                ]
            },
            {
                text: "Tools",
                menuItems: [
                    {commandId: CommandIds.checkAppDependencies},
                    {commandId: CommandIds.stopRunningScripts},
                    {commandId: CommandIds.stopScriptsAndRunners},
                ]
            },
            {
                text: "Help",
                menuItems: [
                    {commandId: CommandIds.openWiki},
                    {commandId: CommandIds.openGitHub},
                    {commandId: CommandIds.searchIssues},
                    {isDivider: true},
                    {commandId: CommandIds.checkForUpdates},
                    {commandId: CommandIds.about},
                ]
            }
        ];

        this.applyCommands(this._items);
        this.updateKeyHints();
        this.updateMenuItems();

        eventBus.subscribeToServer(EnvironmentPropertyChangedEvent, _ => this.updateMenuItems());
        this.keybindingManager.onChanged(() => this.updateKeyHints());
        MonacoEditorUtil.onKeybindingsChanged(() => this.updateKeyHints());

        this.initialized = this.recentScriptsStore.initialize();
        this.recentScriptsStore.onChanged(() => this.applyRecentMenu());
        this.applyRecentMenu();
    }

    /**
     * Fills in what each item takes from its command (its text, icon and tooltip where the item
     * does not override them), and drops items whose command does not exist in this shell.
     */
    private applyCommands(items: IMenuItem[]) {
        for (let i = items.length - 1; i >= 0; i--) {
            const item = items[i];

            if (item.menuItems) {
                this.applyCommands(item.menuItems);
                continue;
            }

            if (!item.commandId) continue;

            const command = this.commandRegistry.get(item.commandId);

            if (!command) {
                items.splice(i, 1);
                continue;
            }

            // A command that has a menu home shares its id with the item, so native menus can keep
            // addressing items by id without the id being stated twice.
            item.id ??= command.id;
            item.text ??= command.title;
            item.icon ??= command.icon;
            item.hoverText ??= command.description;
        }

        this.dropAdjacentDividers(items);
    }

    /**
     * Removes dividers that ended up next to each other, or at either end, once shell-gated items
     * were dropped.
     */
    private dropAdjacentDividers(items: IMenuItem[]) {
        for (let i = items.length - 1; i >= 0; i--) {
            if (!items[i].isDivider) continue;

            if (i === 0 || i === items.length - 1 || items[i - 1].isDivider) {
                items.splice(i, 1);
            }
        }
    }

    private updateKeyHints() {
        let changed = false;

        this.walkItems(this._items, item => {
            if (!item.commandId) return true;

            const keyCombo = this.keybindingManager.getKeybinding(item.commandId)?.keyCombo;
            const command = this.commandRegistry.get(item.commandId);

            // An editor command NetPad has not bound is reached by the editor's own key.
            const keyLabel = keyCombo?.isBound
                ? keyCombo.asString()
                : command?.monacoCommandId
                    ? MonacoEditorUtil.getKeybindingLabel(command.monacoCommandId)
                    : undefined;

            const accelerator = keyCombo?.isBound
                ? keyCombo.asAccelerator()
                : keyLabel?.replaceAll(" ", "");

            if (item.keyLabel !== keyLabel || item.accelerator !== accelerator) {
                item.keyLabel = keyLabel;
                item.accelerator = accelerator;
                changed = true;
            }

            return true;
        });

        if (changed) {
            this.fireChanged();
        }
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
                text: "Clear Recent",
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
        } else if (menuItem.commandId) {
            await this.commandRegistry.execute(menuItem.commandId);
        }
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
        let changed = false;

        this.walkItems(this._items, item => {
            if (!item.commandId) return true;

            const disabled = !this.commandRegistry.isEnabled(item.commandId);

            if (item.disabled !== disabled) {
                item.disabled = disabled;
                changed = true;
            }

            return true;
        });

        if (changed) {
            this.fireChanged();
        }
    }
}
