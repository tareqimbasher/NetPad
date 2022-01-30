import {
    ActiveEnvironmentChanged,
    EnvironmentPropertyChanged,
    EnvironmentsAdded,
    EnvironmentsRemoved,
    IEventBus,
    ISessionApiClient,
    ScriptConfigPropertyChanged,
    ScriptEnvironment,
    ScriptPropertyChanged,
    SessionApiClient
} from "@domain";
import {DI, IHttpClient, ILogger} from "aurelia";

export interface ISession extends ISessionApiClient {
    environments: ScriptEnvironment[];

    get active(): ScriptEnvironment | undefined;

    initialize(): Promise<void>;
}

export const ISession = DI.createInterface<ISession>();

export class Session extends SessionApiClient implements ISession {
    private _active?: ScriptEnvironment | null | undefined;
    private _environments?: ScriptEnvironment[] = [];

    constructor(
        baseUrl: string,
        @IHttpClient http: IHttpClient,
        @IEventBus readonly eventBus: IEventBus,
        @ILogger readonly logger: ILogger) {
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
        this.eventBus.subscribeToServer(EnvironmentsAdded, message => {
            this.logger.debug(`${nameof(EnvironmentsAdded)}`, message);
            this.environments.push(...message.environments);
        });

        this.eventBus.subscribeToServer(EnvironmentsRemoved, message => {
            this.logger.debug(`${nameof(EnvironmentsRemoved)}`, message);

            for (let environment of message.environments) {
                const ix = this.environments.findIndex(e => e.script.id == environment.script.id);
                if (ix >= 0)
                    this.environments.splice(ix, 1);
            }
        });

        this.eventBus.subscribeToServer(ActiveEnvironmentChanged, message => {
            this.logger.debug(`${nameof(ActiveEnvironmentChanged)}`, message);

            const activeScriptId = message.scriptId;

            if (!activeScriptId)
                this._active = null;
            else {
                const environment = this._environments.find(e => e.script.id == activeScriptId);
                if (environment)
                    this._active = environment;
                else
                    this.logger.error(`${nameof(ActiveEnvironmentChanged)}: No environment found with script id: ` + activeScriptId);
            }
        });

        this.eventBus.subscribeToServer(EnvironmentPropertyChanged, message => {
            this.logger.debug(`${nameof(EnvironmentPropertyChanged)}`, message);

            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(EnvironmentPropertyChanged)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            environment[propName] = message.newValue;
        });

        this.eventBus.subscribeToServer(ScriptPropertyChanged, message => {
            this.logger.debug(`${nameof(ScriptPropertyChanged)}`, message);

            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(ScriptPropertyChanged)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const script = environment.script;
            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            script[propName] = message.newValue;
        });

        this.eventBus.subscribeToServer(ScriptConfigPropertyChanged, message => {
            this.logger.debug(`${nameof(ScriptConfigPropertyChanged)}`, message);

            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(ScriptConfigPropertyChanged)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const script = environment.script;
            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            script.config[propName] = message.newValue;
        });
    }
}
