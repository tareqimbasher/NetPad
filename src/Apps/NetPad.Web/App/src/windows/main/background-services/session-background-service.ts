import {IBackgroundService} from "./ibackground-service";
import {ipcRenderer} from "electron";
import {ISession, Session, ScriptEnvironment} from "@domain";

export class SessionBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession) {}

    start(): Promise<void> {
        ipcRenderer.on("session-active-environment-changed", (event, json: string) => {
            const activeScriptId = JSON.parse(json);
            (this.session as Session).internalSetActive(activeScriptId);
        });

        ipcRenderer.on("environment-added", (event, json) => {
            const environments = JSON.parse(json).map(q => ScriptEnvironment.fromJS(q)) as ScriptEnvironment[];
            this.session.environments.push(...environments);
        });

        ipcRenderer.on("environment-removed", (event, json) => {
            const environments = JSON.parse(json).map(q => ScriptEnvironment.fromJS(q)) as ScriptEnvironment[];

            for (let environment of environments) {
                const ix = this.session.environments.findIndex(e => e.script.id == environment.script.id);
                if (ix >= 0)
                    this.session.environments.splice(ix, 1);
            }
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("environment-added");
        ipcRenderer.removeAllListeners("environment-removed");
    }
}
