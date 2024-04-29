import {Socket} from "socket.io";
import {Connector} from "./connector";
import {MainMenuManager} from "./app/main-menu/main-menu-manager";
import {WindowControlsManager} from "./app/window-controls/window-controls-manager";
import {LinkNavigationHandler} from "./app/link-navigation-handler";

export class HookService extends Connector {
    constructor(socket: Socket, app: Electron.App) {
        super(socket, app);
    }

    public onHostReady(): void {
        WindowControlsManager.init();
        MainMenuManager.init();
        LinkNavigationHandler.init();
    }
}
