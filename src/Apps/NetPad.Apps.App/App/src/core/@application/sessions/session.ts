import {ILogger} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {
    ActiveEnvironmentChangedEvent,
    EnvironmentPropertyChangedEvent,
    EnvironmentsAddedEvent,
    EnvironmentsRemovedEvent,
    IEventBus,
    ISession,
    ScriptConfigPropertyChangedEvent,
    ScriptEnvironment,
    ScriptPropertyChangedEvent,
    SessionApiClient
} from "@application";

export class Session extends SessionApiClient implements ISession {
    public readonly environments: ScriptEnvironment[] = [];

    private _active?: ScriptEnvironment | null | undefined;
    private readonly logger: ILogger;

    constructor(
        baseUrl: string,
        @IHttpClient http: IHttpClient,
        @IEventBus readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(baseUrl, http);
        this.logger = logger.scopeTo(nameof(Session));
        this.subscribeToEvents();
    }

    public get active(): ScriptEnvironment | null | undefined {
        return this._active;
    }

    public async initialize(): Promise<void> {
        const environments = await super.getEnvironments();
        this.environments.push(...environments);

        const activeScriptId = await this.getActive();
        if (activeScriptId) {
            this._active = this.environments.find(e => e.script.id === activeScriptId);
        }
    }

    public getScriptName(scriptId: string): string | undefined {
        return this.environments.find(e => e.script.id === scriptId)?.script.name;
    }

    public override async getEnvironment(scriptId: string): Promise<ScriptEnvironment> {
        let env = this.environments.find(e => e.script.id === scriptId);
        if (!env) {
            // if not found in cached env's get it form API and cache it
            env = await super.getEnvironment(scriptId);
            this.environments.push(env);
        }

        return env;
    }

    public override async getEnvironments(): Promise<ScriptEnvironment[]> {
        return this.environments;
    }

    public override async activate(scriptId: string, signal?: AbortSignal | undefined): Promise<void> {
        if (scriptId === this.active?.script.id) return;
        return super.activate(scriptId, signal);
    }

    private subscribeToEvents() {
        this.eventBus.subscribeToServer(EnvironmentsAddedEvent, message => {
            this.environments.push(...message.environments);
        });

        this.eventBus.subscribeToServer(EnvironmentsRemovedEvent, message => {
            for (const environment of message.environments) {
                const ix = this.environments.findIndex(e => e.script.id == environment.script.id);
                if (ix >= 0)
                    this.environments.splice(ix, 1);
            }
        });

        this.eventBus.subscribeToServer(ActiveEnvironmentChangedEvent, message => {
            const activeScriptId = message.scriptId;

            if (!activeScriptId)
                this._active = null;
            else {
                const environment = this.environments.find(e => e.script.id == activeScriptId);
                if (environment)
                    this._active = environment;
                else
                    this.logger.error(`${nameof(ActiveEnvironmentChangedEvent)}: No environment found with script id: ` + activeScriptId);
            }
        });


        this.eventBus.subscribeToServer(EnvironmentPropertyChangedEvent, message => {
            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(EnvironmentPropertyChangedEvent)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            environment[propName as keyof typeof environment] = message.newValue as never;
        });

        this.eventBus.subscribeToServer(ScriptPropertyChangedEvent, message => {
            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(ScriptPropertyChangedEvent)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const script = environment.script;
            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            script[propName as keyof typeof script] = message.newValue as never;
        });

        this.eventBus.subscribeToServer(ScriptConfigPropertyChangedEvent, message => {
            const environment = this.environments.find(e => e.script.id == message.scriptId);

            if (!environment) {
                this.logger.error(`${nameof(ScriptConfigPropertyChangedEvent)}: Could not find an environment for script id: ` + message.scriptId);
                return;
            }

            const script = environment.script;
            const propName = message.propertyName.charAt(0).toLowerCase() + message.propertyName.slice(1);
            script.config[propName as keyof typeof script.config] = message.newValue as never;
        });
    }
}
