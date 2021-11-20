import {ISession} from "@domain";
import {ipcRenderer} from "electron";
import {IBackgroundService} from "./ibackground-service";

export class ScriptBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession) {}

    public start(): Promise<void> {
        ipcRenderer.on("script-property-changed", (event, json) =>
        {
            const update = JSON.parse(json);
            const script = this.session.scripts.find(s => s.id == update.scriptId);
            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            script[propName] = update.newValue;
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("script-property-changed");
    }
}
