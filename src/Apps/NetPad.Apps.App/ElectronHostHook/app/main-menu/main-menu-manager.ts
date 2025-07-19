import {BrowserWindow, ipcMain, Menu, MenuItemConstructorOptions} from "electron";
import {AppMenuItemWalker, IAppMenuItem} from "./models";
import {electronConstants} from "../../electron-shared";

const isMac = process.platform === "darwin";

export class ClickMenuItemCommand {
    constructor(public readonly menuItemId: string) {
    }
}


export class MainMenuManager {
    private static appMenuItems: IAppMenuItem[] = [];

    public static init() {
        const mainMenuBootstrapChannelName = electronConstants.ipcEventNames.mainMenuBootstrap;

        const sendBootstrapEvent = () => {
            let allBrowsers: Electron.BrowserWindow[];

            try {
                allBrowsers = BrowserWindow.getAllWindows();

                if (allBrowsers.length < 1) {
                    console.warn(`There are no active browser windows to send '${mainMenuBootstrapChannelName}'`);
                    return;
                }
            } catch (err) {
                console.error("Unexpected error evaluating browser windows: ", err);
            }

            try {
                allBrowsers[0].webContents.send(mainMenuBootstrapChannelName);
            } catch (err) {
                // ignore, Renderer process event handler might not be setup yet.
            }
        };

        ipcMain.handle(mainMenuBootstrapChannelName, (event, ...args) => {
            const message = args.length >= 1 ? args[0] : null;

            this.appMenuItems = message?.menuItems || [];

            this.rebuildMenu();
        });

        ipcMain.handle(electronConstants.ipcEventNames.appActivated, (event, ...args) => sendBootstrapEvent());

        // Send right away to take care of any race-condition that might occur.
        sendBootstrapEvent();
    }

    private static rebuildMenu() {
        const menu = Menu.buildFromTemplate(this.buildTemplate());
        Menu.setApplicationMenu(menu);
    }

    private static fromAppMenuItem(id: string): MenuItemConstructorOptions | undefined {
        const item = AppMenuItemWalker.find(this.appMenuItems, item => item.id === id);
        if (!item) return {type: "separator"};

        if (item.isDivider) {
            return {type: "separator"};
        }

        return <MenuItemConstructorOptions>{
            id: id,
            label: item.text,
            accelerator: this.getAccelerator(item),
            click: async (menuItem, browserWindow) => {
                if (browserWindow instanceof BrowserWindow) {
                    await this.sendMenuItemToRenderer(menuItem.id, browserWindow);
                }
            },
        };
    }

    private static buildTemplate(): Partial<MenuItemConstructorOptions>[] {
        return [
            // { role: 'appMenu' }
            ...(isMac
                ? [{
                    label: "NetPad",
                    submenu: <MenuItemConstructorOptions[]>[
                        {role: 'about'},
                        {type: "separator"},
                        {role: 'services'},
                        {type: "separator"},
                        {role: 'hide'},
                        {role: 'hideOthers'},
                        {role: 'unhide'},
                        {type: "separator"},
                        {role: 'quit'}
                    ]
                }]
                : []),
            // { role: 'fileMenu' }
            {
                label: 'File',
                submenu: [
                    this.fromAppMenuItem("file.new"),
                    this.fromAppMenuItem("file.goToScript"),
                    {type: "separator"},
                    this.fromAppMenuItem("file.save"),
                    this.fromAppMenuItem("file.saveAll"),
                    this.fromAppMenuItem("file.properties"),
                    this.fromAppMenuItem("file.close"),
                    {type: "separator"},
                    this.fromAppMenuItem("file.settings"),
                    ...<MenuItemConstructorOptions[]>(isMac ? [] : [{role: "quit", label: "Exit"}])
                ]
            },
            // { role: 'editMenu' }
            {
                label: 'Edit',
                submenu: <MenuItemConstructorOptions[]>[
                    {role: 'undo'},
                    {role: 'redo'},
                    {type: "separator"},
                    {role: 'cut'},
                    {role: 'copy'},
                    {role: 'paste'},
                    ...(isMac
                        ? [
                            {role: 'pasteAndMatchStyle'},
                            {role: 'delete'},
                            {type: "separator"},
                            {role: 'selectAll'},
                            {type: "separator"},
                            {
                                label: 'Speech',
                                submenu: [
                                    {role: 'startSpeaking'},
                                    {role: 'stopSpeaking'}
                                ]
                            }
                        ]
                        : [
                            {role: 'delete'},
                            {type: "separator"},
                            {role: 'selectAll'}
                        ]),
                    {type: "separator"},
                    this.fromAppMenuItem("edit.find"),
                    this.fromAppMenuItem("edit.replace"),
                    {type: "separator"},
                    this.fromAppMenuItem("edit.transform1"),
                    this.fromAppMenuItem("edit.transform2"),
                    this.fromAppMenuItem("edit.transform3"),
                    this.fromAppMenuItem("edit.transform4"),
                    this.fromAppMenuItem("edit.transform5"),
                    this.fromAppMenuItem("edit.transform6"),
                    {type: "separator"},
                    this.fromAppMenuItem("edit.toggleLineComment"),
                    this.fromAppMenuItem("edit.toggleBlockComment"),
                ]
            },
            // { role: 'viewMenu' }
            {
                label: 'View',
                submenu: [
                    this.fromAppMenuItem("view.explorer"),
                    this.fromAppMenuItem("view.output"),
                    this.fromAppMenuItem("view.code"),
                    this.fromAppMenuItem("view.namespaces"),
                    {type: "separator"},
                    {role: 'reload'},
                    {role: 'forceReload'},
                    {role: 'toggleDevTools'},
                    {type: "separator"},
                    {role: 'zoomIn'},
                    {role: 'zoomOut'},
                    {role: 'resetZoom'},
                    {type: "separator"},
                    {role: 'togglefullscreen'}
                ]
            },
            {
                label: 'Tools',
                submenu: [
                    this.fromAppMenuItem("tools.dependencyCheck"),
                    this.fromAppMenuItem("tools.stopRunningScripts"),
                    this.fromAppMenuItem("tools.stopScriptHosts"),
                ]
            },
            {role: 'windowMenu'},
            {
                role: 'help',
                submenu: [
                    this.fromAppMenuItem("help.wiki"),
                    this.fromAppMenuItem("help.github"),
                    this.fromAppMenuItem("help.searchIssues"),
                    {type: "separator"},
                    this.fromAppMenuItem("help.checkForUpdates"),
                    this.fromAppMenuItem("help.about"),
                ]
            }
        ];
    }

    private static async sendMenuItemToRenderer(menuItemId: string, browserWindow: Electron.BrowserWindow) {
        browserWindow.webContents.send(ClickMenuItemCommand.name, new ClickMenuItemCommand(menuItemId));
    }

    private static getAccelerator(menuItem: IAppMenuItem): string | undefined {
        if (menuItem.shortcut) {
            const combo = [...menuItem.shortcut.keyCombo];
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
