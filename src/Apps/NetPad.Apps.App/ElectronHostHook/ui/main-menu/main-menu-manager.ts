import {app, BrowserWindow, ipcMain, Menu, MenuItemConstructorOptions} from "electron";
import {AppMenuItemWalker, IAppMenuItem, IAppShortcut} from "./models";

const isMac = process.platform === "darwin";

export class ClickMenuItemEvent {
    constructor(public readonly menuItemId: string) {
    }
}


export class MainMenuManager {
    private static appMenuItems: IAppMenuItem[] = [];

    public static init() {
        const mainMenuBootstrapChannelName = "main-menu-bootstrap";

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

        ipcMain.handle("AppActivatedEvent", (event, ...args) => sendBootstrapEvent());

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

        return {
            id: id,
            label: item.text,
            accelerator: item.shortcut ? this.getAccelerator(item.shortcut) : item.helpText?.replaceAll(" ", "") || undefined,
            click: async (menuItem, browserWindow) => await this.sendMenuItemToRenderer(menuItem.id, browserWindow)
        };
    }

    private static buildTemplate(): Partial<MenuItemConstructorOptions>[] {
        return [
            // { role: 'appMenu' }
            ...(isMac
                ? [{
                    label: app.name,
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
                    this.fromAppMenuItem("view.output"),
                    this.fromAppMenuItem("view.explorer"),
                    this.fromAppMenuItem("view.namespaces"),
                    {type: "separator"},
                    {role: 'reload'},
                    {role: 'forceReload'},
                    {role: 'toggleDevTools'},
                    {type: "separator"},
                    {role: 'resetZoom'},
                    {role: 'zoomIn'},
                    {role: 'zoomOut'},
                    {type: "separator"},
                    {role: 'togglefullscreen'}
                ]
            },
            // { role: 'windowMenu' }
            {
                label: 'Window',
                submenu: <MenuItemConstructorOptions[]>[
                    {role: 'minimize'},
                    {role: 'zoom'},
                    ...(isMac
                        ? [
                            {type: "separator"},
                            {role: 'front'},
                            {type: "separator"},
                            {role: 'window'}
                        ]
                        : [])
                ]
            },
            {
                role: 'help',
                submenu: [
                    this.fromAppMenuItem("help.about"),
                    this.fromAppMenuItem("help.checkForUpdates"),
                    this.fromAppMenuItem("help.github"),
                    this.fromAppMenuItem("help.searchIssues"),
                ]
            }
        ];
    }

    private static async sendMenuItemToRenderer(menuItemId: string, browserWindow: Electron.BrowserWindow) {
        browserWindow.webContents.send(ClickMenuItemEvent.name, new ClickMenuItemEvent(menuItemId));
    }

    private static getAccelerator(shortcut: IAppShortcut): string {
        const combo = [...shortcut.keyCombo];
        let accelerator: string[] = [];

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
}
