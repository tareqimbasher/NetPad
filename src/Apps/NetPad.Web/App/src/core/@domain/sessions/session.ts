import {ISessionService, SessionService, ScriptEnvironment} from "@domain";
import {DI} from "aurelia";

export interface ISession extends ISessionService {
    environments: ScriptEnvironment[];

    get active(): ScriptEnvironment | undefined;

    initialize(): Promise<void>;
}

export const ISession = DI.createInterface<ISession>();

export class Session extends SessionService implements ISession {
    private _active?: ScriptEnvironment | null | undefined;
    private _environments?: ScriptEnvironment[] = [];

    public get environments(): ScriptEnvironment[] {
        return this._environments;
    }

    public get active(): ScriptEnvironment | null | undefined {
        return this._active;
    }

    public async initialize(): Promise<void> {
        const environments = await this.getEnvironments();
        this.environments.push(...environments);

        const activeScriptId = await this.getActive();
        if (activeScriptId) {
            this._active = this._environments.find(e => e.script.id === activeScriptId);
        }
    }

    public internalSetActive(scriptId: string | null) {
        if (!scriptId)
            this._active = null;
        else {
            const environment = this._environments.find(e => e.script.id == scriptId);
            if (environment)
                this._active = environment;
            else
                console.error("No environment found with script id: " + scriptId);
        }
    }
}
