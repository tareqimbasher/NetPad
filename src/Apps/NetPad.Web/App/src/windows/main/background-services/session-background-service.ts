import {IBackgroundService} from "./ibackground-service";
import {ipcRenderer} from "electron";
import {ISession, Script} from "@domain";

export class SessionBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession) {}

    start(): Promise<void> {
        ipcRenderer.on("session-script-added", (event, json) => {
            const scripts = JSON.parse(json).map(q => Script.fromJS(q)) as Script[];
            this.session.add(...scripts);
        });

        ipcRenderer.on("session-script-removed", (event, json) => {
            const scripts = JSON.parse(json).map(q => Script.fromJS(q)) as Script[];
            this.session.remove(...scripts);
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("session-script-added");
        ipcRenderer.removeAllListeners("session-script-removed");
    }
}
