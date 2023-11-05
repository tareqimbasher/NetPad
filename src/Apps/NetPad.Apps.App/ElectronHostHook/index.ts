import {Socket} from "socket.io";
import {Connector} from "./connector";
import {MainMenuManager} from "./ui/main-menu/main-menu-manager";
import {WindowControlsManager} from "./ui/window-controls/window-controls-manager";

export class HookService extends Connector {
    constructor(socket: Socket, app: Electron.App) {
        super(socket, app);
    }

    public onHostReady(): void {
        WindowControlsManager.init();
        MainMenuManager.init();
    }
}
