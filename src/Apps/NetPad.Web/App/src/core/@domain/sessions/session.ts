import {
    ISessionService,
    SessionService,
    ScriptEnvironment,
    IEventBus,
    ScriptPropertyChanged,
    EnvironmentsAdded, EnvironmentsRemoved, EnvironmentPropertyChanged, ActiveEnvironmentChanged
} from "@domain";
import {DI, IHttpClient} from "aurelia";

export interface ISession extends ISessionService {
    environments: ScriptEnvironment[];

    get active(): ScriptEnvironment | undefined;

    initialize(): Promise<void>;
}

export const ISession = DI.createInterface<ISession>();

export class Session extends SessionService implements ISession {
    private _active?: ScriptEnvironment | null | undefined;
    private _environments?: ScriptEnvironment[] = [];

    constructor(
        baseUrl: string,
        @IHttpClient http: IHttpClient,
        @IEventBus readonly eventBus: IEventBus) {
        super(baseUrl, http);
        this.subscribeToEvents();
    }

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

    private subscribeToEvents() {
        this.eventBus.subscribeRemote(EnvironmentsAdded, message =>
        {
            this.environments.push(...message.environments);
        });

        this.eventBus.subscribeRemote(EnvironmentsRemoved, message =>
        {
            for (let environment of message.environments) {
                const ix = this.environments.findIndex(e => e.script.id == environment.script.id);
                if (ix >= 0)
                    this.environments.splice(ix, 1);
            }
        });

        this.eventBus.subscribeRemote(ActiveEnvironmentChanged, change =>
        {
            const activeScriptId = change.scriptId;

            if (!activeScriptId)
                this._active = null;
            else {
                const environment = this._environments.find(e => e.script.id == activeScriptId);
                if (environment)
                    this._active = environment;
                else
                    console.error("No environment found with script id: " + activeScriptId);
            }
        });

        this.eventBus.subscribeRemote(EnvironmentPropertyChanged, update =>
        {
            const environment = this.environments.find(e => e.script.id == update.scriptId);

            if (!environment) {
                console.error("Could not find an environment for script id: " + update.scriptId);
                return;
            }

            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            environment[propName] = update.newValue;
        });

        this.eventBus.subscribeRemote(ScriptPropertyChanged, update =>
        {
            const environment = this.environments.find(e => e.script.id == update.scriptId);

            if (!environment) {
                console.error("Could not find an environment for script id: " + update.scriptId);
                return;
            }

            const script = environment.script;
            const propName = update.propertyName.charAt(0).toLowerCase() + update.propertyName.slice(1);
            script[propName] = update.newValue;
        });
    }
}
