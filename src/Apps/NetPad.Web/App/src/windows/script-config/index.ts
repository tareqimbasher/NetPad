import {IScriptManager, ISession, Script, ScriptEnvironment} from "@domain";

export class Index {
    public environment: ScriptEnvironment;
    public namespaces: string;

    public get script(): Script {
        return this.environment.script;
    }

    constructor(
        readonly startupOptions: URLSearchParams,
        @ISession readonly session: ISession,
        @IScriptManager readonly scriptManager: IScriptManager) {
    }

    public async binding() {
        this.environment = await this.session.getEnvironment(this.startupOptions.get("script-id"));
        document.title = `${this.script.name} - Properties`;
        this.namespaces = this.script.config.namespaces.join('\n');
    }

    public async ok() {
        try {
            const config = this.script.config;
            config.namespaces = this.namespaces
                .split('\n')
                .map(ns => ns.trim())
                .filter(ns => ns);

            await this.scriptManager.setConfig(this.script.id, config);
            window.close();
        }
        catch (ex) {
            alert(ex);
        }
    }

    public cancel() {
        window.close();
    }
}
