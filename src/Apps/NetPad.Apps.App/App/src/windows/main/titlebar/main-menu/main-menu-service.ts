import {DI} from "aurelia";
import {IMenuItem} from "./imenu-item";
import {System} from "@common";
import {IShortcutManager} from "@application";
import {ITextEditorService} from "@application/editor/text-editor-service";
import {ISettingService, IWindowService} from "@domain";

export const IMainMenuService = DI.createInterface<MainMenuService>();

export interface IMainMenuService {
    items: ReadonlyArray<IMenuItem>;
    addItem(item: IMenuItem);
    removeItem(item: IMenuItem);
}

export class MainMenuService implements IMainMenuService {
    private readonly _items: IMenuItem[] = [];

    constructor(
        @ISettingService private readonly settingsService: ISettingService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ITextEditorService private readonly textEditorService: ITextEditorService,
        @IWindowService private readonly windowService: IWindowService
    ) {
        this._items = [
            {
                text: "File",
                menuItems: [
                    {
                        text: "New",
                        icon: "add-script-icon",
                        shortcut: this.shortcutManager.getShortcutByName("New"),
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Save",
                        icon: "save-icon",
                        shortcut: this.shortcutManager.getShortcutByName("Save"),
                    },
                    {
                        text: "Save All",
                        icon: "save-icon",
                        shortcut: this.shortcutManager.getShortcutByName("Save All"),
                    },
                    {
                        text: "Properties",
                        icon: "script-properties-icon",
                        shortcut: this.shortcutManager.getShortcutByName("Script Properties"),
                    },
                    {
                        text: "Close",
                        icon: "close-icon",
                        shortcut: this.shortcutManager.getShortcutByName("Close"),
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Settings",
                        icon: "settings-icon",
                        shortcut: this.shortcutManager.getShortcutByName("Settings"),
                    },
                    {
                        text: "Exit",
                        click: async () => window.close()
                    }
                ]
            },
            {
                text: "Edit",
                menuItems: [
                    {
                        text: "Undo",
                        icon: "undo-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "undo", null)
                    },
                    {
                        text: "Redo",
                        icon: "redo-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "redo", null)
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Cut",
                        icon: "cut-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.clipboardCutAction", null)
                    },
                    {
                        text: "Copy",
                        icon: "copy-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.clipboardCopyAction", null)
                    },
                    {
                        text: "Delete",
                        icon: "backspace-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "deleteRight", null)
                    },
                    {
                        text: "Select All",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.selectAll", null)
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Find",
                        icon: "search-icon",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "actions.findWithSelection", null)
                    },
                    {
                        text: "Replace",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.startFindReplaceAction", null)
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Transform to Upper Case",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToUppercase", null)
                    },
                    {
                        text: "Transform to Lower Case",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToLowercase", null)
                    },
                    {
                        text: "Transform to Title Case",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToTitlecase", null)
                    },
                    {
                        text: "Transform to Kebab Case",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToKebabcase", null)
                    },
                    {
                        text: "Transform to Snake Case",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.transformToSnakecase", null)
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Toggle Line Comment",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.commentLine", null)
                    },
                    {
                        text: "Toggle Block Comment",
                        click: async () => this.textEditorService.active?.monaco
                            .trigger(null, "editor.action.blockComment", null)
                    },
                ]
            },
            {
                text: "View",
                menuItems: [
                    {
                        text: "Reload",
                        click: async () => window.location.reload()
                    },
                    {
                        text: "Open Developer Tools",
                        click: async() => this.windowService.openDeveloperTools()
                    },
                    {
                        isDivider: true
                    },
                    {
                        text: "Zoom In",
                        icon: "zoom-in-icon",
                        helpText: "Ctrl + +",
                    },
                    {
                        text: "Zoom Out",
                        icon: "zoom-out-icon",
                        helpText: "Ctrl + -",
                    },
                    {
                        text: "Reset Zoom",
                        helpText: "Ctrl + 0",
                    },
                ]
            },
            {
                text: "Help",
                menuItems: [
                    {
                        text: "About",
                        icon: "star-icon",
                        click: async () => await this.settingsService.openSettingsWindow("about")
                    },
                    {
                        text: "GitHub",
                        icon: "github-icon",
                        click: async () => System.openUrlInBrowser("https://github.com/tareqimbasher/NetPad")
                    },
                    {
                        text: "Search Issues",
                        icon: "github-icon",
                        click: async () => System.openUrlInBrowser("https://github.com/tareqimbasher/NetPad/issues")
                    },
                ]
            }
        ];
    }

    public get items(): ReadonlyArray<IMenuItem> {
        return this._items;
    }

    public addItem(item: IMenuItem) {
        this._items.push(item);
    }

    public removeItem(item: IMenuItem) {
        const ix = this._items.indexOf(item);
        if (ix >= 0) this._items.splice(ix, 1);
    }
}
