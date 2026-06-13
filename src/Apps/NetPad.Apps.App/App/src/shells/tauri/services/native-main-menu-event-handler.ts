import {ILogger} from "aurelia";
import {Util, WithDisposables} from "@common";
import {IBackgroundService, IShortcutManager} from "@application";
import {IMainMenuService} from "@application/main-menu/imain-menu-service";
import {IMenuItem} from "@application/main-menu/imenu-item";
import {Menu, MenuItemOptions, PredefinedMenuItemOptions, Submenu, SubmenuOptions} from "@tauri-apps/api/menu"
import {Window} from "@tauri-apps/api/window"
import {MenuItem} from "@tauri-apps/api/menu/menuItem";
import {PredefinedMenuItem} from "@tauri-apps/api/menu/predefinedMenuItem";
import {invoke} from "@tauri-apps/api/core";

/**
 * Manages the Tauri main menu.
 */
export class NativeMainMenuEventHandler extends WithDisposables implements IBackgroundService {
    private readonly logger: ILogger;
    private isMac?: boolean;

    private readonly rebuildMenu = Util.debounceAsync(this, async () => {
        try {
            await this.buildAndSetMenu();
        } catch (err) {
            this.logger.error("Failed to build and set native menu:", err);
        }
    }, 150, true);

    constructor(
        @IMainMenuService private readonly mainMenuService: IMainMenuService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ILogger logger: ILogger) {
        super();
        this.logger = logger.scopeTo(nameof(NativeMainMenuEventHandler));
    }

    public async start(): Promise<void> {
        if (!this.mainMenuService) {
            return Promise.resolve();
        }

        this.addDisposable(this.mainMenuService.onChanged(() => this.rebuildMenu()));

        try {
            await this.mainMenuService.initialized;
        } catch (err) {
            this.logger.error("Main menu initial hydration failed; building native menu anyway:", err);
        }

        this.rebuildMenu();
    }

    private async buildAndSetMenu(): Promise<void> {
        const appMenuItems = new Map<string, IMenuItem>();
        for (const top of this.mainMenuService!.items) {
            if (top.menuItems && top.menuItems.length > 0) {
                for (const child of top.menuItems) {
                    if (child.id) appMenuItems.set(child.id, child);
                }
            } else if (top.id) {
                appMenuItems.set(top.id, top);
            }
        }

        this.isMac ??= await invoke("get_os_type") === "macos";

        const menu = await Menu.new({
            items: [
                this.isMac ? await Submenu.new({
                    text: "NetPad",
                    items: [
                        await PredefinedMenuItem.new({
                            item: {
                                About: {
                                    name: "NetPad",
                                    authors: ["Tareq Imbasher"],
                                    copyright: "Copyright (C) 2022 Tareq Imbasher",
                                    website: "https://netpad.dev",
                                }
                            }
                        }),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await PredefinedMenuItem.new({item: "Services"}),
                        await PredefinedMenuItem.new({item: "Hide"}),
                        await PredefinedMenuItem.new({item: "HideOthers"}),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await PredefinedMenuItem.new({item: "Quit"}),
                    ]
                }) : undefined!,
                await Submenu.new({
                    id: "file",
                    text: "File",
                    items: [
                        await this.fromAppMenuItem(appMenuItems, "file.new"),
                        await this.fromAppMenuItem(appMenuItems, "file.open"),
                        await this.fromAppMenuItem(appMenuItems, "file.openRecent"),
                        await this.fromAppMenuItem(appMenuItems, "file.goToScript"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "file.save"),
                        await this.fromAppMenuItem(appMenuItems, "file.saveAs"),
                        await this.fromAppMenuItem(appMenuItems, "file.saveAll"),
                        await this.fromAppMenuItem(appMenuItems, "file.properties"),
                        await this.fromAppMenuItem(appMenuItems, "file.close"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "file.settings"),
                        this.isMac ? undefined : await PredefinedMenuItem.new({item: "Quit", text: "Exit"})
                    ].filter(x => x).map(x => x!)
                }),
                await Submenu.new({
                    id: "edit",
                    text: "Edit",
                    items: [
                        await this.fromAppMenuItem(appMenuItems, "edit.undo"),
                        await this.fromAppMenuItem(appMenuItems, "edit.redo"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        ...!this.isMac ? [] : [
                            await PredefinedMenuItem.new({item: "Cut"}),
                            await PredefinedMenuItem.new({item: "Copy"}),
                            await PredefinedMenuItem.new({item: "Paste"}),
                            await PredefinedMenuItem.new({item: "Separator"}),
                        ],
                        await this.fromAppMenuItem(appMenuItems, "edit.selectAll"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "edit.find"),
                        await this.fromAppMenuItem(appMenuItems, "edit.replace"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform1"),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform2"),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform3"),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform4"),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform5"),
                        await this.fromAppMenuItem(appMenuItems, "edit.transform6"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "edit.toggleLineComment"),
                        await this.fromAppMenuItem(appMenuItems, "edit.toggleBlockComment"),
                    ].filter(x => x).map(x => x!)
                }),
                await Submenu.new({
                    id: "view",
                    text: "View",
                    items: [
                        await this.fromAppMenuItem(appMenuItems, "view.explorer"),
                        await this.fromAppMenuItem(appMenuItems, "view.output"),
                        await this.fromAppMenuItem(appMenuItems, "view.code"),
                        await this.fromAppMenuItem(appMenuItems, "view.namespaces"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "view.reload"),
                        await this.fromAppMenuItem(appMenuItems, "view.toggleDeveloperTools"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "view.zoomIn"),
                        await this.fromAppMenuItem(appMenuItems, "view.zoomOut"),
                        await this.fromAppMenuItem(appMenuItems, "view.resetZoom"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "view.fullScreen"),
                    ].filter(x => x).map(x => x!)
                }),
                await Submenu.new({
                    id: "tools",
                    text: "Tools",
                    items: [
                        await this.fromAppMenuItem(appMenuItems, "tools.dependencyCheck"),
                        await this.fromAppMenuItem(appMenuItems, "tools.stopRunningScripts"),
                        await this.fromAppMenuItem(appMenuItems, "tools.stopScriptHosts"),
                    ].filter(x => x).map(x => x!)
                }),
                await Submenu.new({
                    id: "help",
                    text: "Help",
                    items: [
                        await this.fromAppMenuItem(appMenuItems, "help.wiki"),
                        await this.fromAppMenuItem(appMenuItems, "help.github"),
                        await this.fromAppMenuItem(appMenuItems, "help.searchIssues"),
                        await PredefinedMenuItem.new({item: "Separator"}),
                        await this.fromAppMenuItem(appMenuItems, "help.checkForUpdates"),
                        await this.fromAppMenuItem(appMenuItems, "help.about"),
                    ].filter(x => x).map(x => x!)
                })
            ].filter(x => x)
        });

        if (this.isMac) {
            await menu.setAsAppMenu();
        } else {
            await menu.setAsWindowMenu(Window.getCurrent());
        }
    }

    public stop(): void {
        this.dispose();
    }

    private async fromAppMenuItem(appMenuItems: Map<string, IMenuItem>, id: string): Promise<Submenu | MenuItem | PredefinedMenuItem | undefined> {
        const menuItem = appMenuItems.get(id);

        if (!menuItem) {
            this.logger.error("Could not find menu item with id: ", id);
            return undefined;
        }

        if (menuItem.isDivider) {
            return await PredefinedMenuItem.new(<PredefinedMenuItemOptions>{
                item: "Separator"
            });
        } else if (menuItem.menuItems) {
            return await this.buildSubmenu(menuItem);
        } else {
            return await MenuItem.new(<MenuItemOptions>{
                id: menuItem.id,
                text: menuItem.text,
                accelerator: this.getAccelerator(menuItem),
                action: id => {
                    if (menuItem.click) {
                        menuItem.click();
                    } else if (menuItem.shortcut) {
                        this.shortcutManager.executeShortcut(menuItem.shortcut);
                    }
                }
            });
        }
    }

    private async buildSubmenu(parent: IMenuItem): Promise<Submenu> {
        const items: (Submenu | MenuItem | PredefinedMenuItem)[] = [];

        for (const child of parent.menuItems ?? []) {
            if (child.isDivider) {
                items.push(await PredefinedMenuItem.new({item: "Separator"}));
            } else if (child.menuItems) {
                items.push(await this.buildSubmenu(child));
            } else {
                items.push(await MenuItem.new(<MenuItemOptions>{
                    id: child.id,
                    text: child.text ?? "",
                    accelerator: this.getAccelerator(child),
                    action: () => {
                        if (child.click) {
                            child.click();
                        } else if (child.shortcut) {
                            this.shortcutManager.executeShortcut(child.shortcut);
                        }
                    }
                }));
            }
        }

        return await Submenu.new(<SubmenuOptions>{
            id: parent.id,
            text: parent.text ?? "",
            enabled: !parent.disabled,
            items
        });
    }

    private getAccelerator(menuItem: IMenuItem): string | undefined {
        if (menuItem.shortcut) {
            const combo = [...menuItem.shortcut.keyCombo.asArray];
            const accelerator: string[] = [];

            for (const part of combo) {
                const lower = part.toLowerCase();
                if (lower === "meta") {
                    accelerator.push("Meta");
                } else if (lower === "alt") {
                    accelerator.push("Alt");
                } else if (lower === "ctrl") {
                    accelerator.push("CmdOrCtrl");
                } else if (lower === "shift") {
                    accelerator.push("Shift");
                } else {
                    accelerator.push(part);
                }
            }

            return accelerator.join("+");
        }

        return menuItem.helpText?.replaceAll(" ", "");
    }
}
