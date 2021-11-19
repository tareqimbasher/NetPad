import {IBackgroundService} from "./ibackground-service";
import {ipcRenderer} from "electron";
import {ISession, Query} from "@domain";

export class SessionBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession) {}

    start(): Promise<void> {
        ipcRenderer.on("session-query-added", (event, json) => {
            const queries = JSON.parse(json).map(q => Query.fromJS(q)) as Query[];
            this.session.add(...queries);
        });

        ipcRenderer.on("session-query-removed", (event, json) => {
            const queries = JSON.parse(json).map(q => Query.fromJS(q)) as Query[];
            this.session.remove(...queries);
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("session-query-added");
        ipcRenderer.removeAllListeners("session-query-removed");
    }
}
