import {ISession} from "@domain";
import {ipcRenderer} from "electron";
import {IBackgroundService} from "./ibackground-service";
import {IEventAggregator} from "aurelia";

export class ScriptBackgroundService implements IBackgroundService{
    constructor(@ISession readonly session: ISession, @IEventAggregator readonly eventBus: IEventAggregator) {}

    public start(): Promise<void> {
        ipcRenderer.on("environment-property-changed", (event, json) =>
        {
            const update = JSON.parse(json);
            const environment = this.session.environments.find(e => e.script.id == update.scriptId);

            if (!environment) {
                console.error("Could not find an environment for script id: " + update.scriptId);
                return;
            }

            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            environment[propName] = update.newValue;
        });

        ipcRenderer.on("script-property-changed", (event, json) =>
        {
            const update = JSON.parse(json);
            const environment = this.session.environments.find(e => e.script.id == update.scriptId);

            if (!environment) {
                console.error("Could not find an environment for script id: " + update.scriptId);
                return;
            }

            const script = environment.script;
            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            script[propName] = update.newValue;
        });

        ipcRenderer.on("script-results", (event, json) =>
        {
            this.eventBus.publish("script-results", JSON.parse(json));
        });

        return Promise.resolve();
    }

    public stop(): void {
        ipcRenderer.removeAllListeners("script-property-changed");
    }
}
