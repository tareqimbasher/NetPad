import {ISession} from "@domain";
import {ipcRenderer} from "electron";
import {IBackgroundService} from "./ibackground-service";

export class QueryBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession) {}

    public start(): Promise<void> {
        ipcRenderer.on("query-property-changed", (event, json) =>
        {
            const update = JSON.parse(json);
            const query = this.session.queries.find(s => s.id == update.queryId);
            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            query[propName] = update.newValue;
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("query-property-changed");
    }
}
