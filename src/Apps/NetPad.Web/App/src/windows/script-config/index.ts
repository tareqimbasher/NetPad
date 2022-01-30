import {IScriptService, ISession, Script, Settings} from "@domain";
import {ConfigStore} from "./config-store";

export class Index {
    public script: Script;
    public selectedTab;
    public tabs = [
        {route: "references", text: "References"},
        {route: "packages", text: "Packages"},
        {route: "namespaces", text: "Namespaces"},
    ];

    constructor(
        readonly startupOptions: URLSearchParams,
        readonly settings: Settings,
        readonly configStore: ConfigStore,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService) {
        this.selectedTab = this.tabs[0];
    }

    public async binding() {
        const environment = await this.session.getEnvironment(this.startupOptions.get("script-id"));
        this.script = environment.script;

        document.title = `${this.script.name} - Properties`;

        this.configStore.namespaces = environment.script.config.namespaces;
        this.configStore.references = environment.script.config.references;
    }

    public async save() {
        try {
            await this.scriptService.setScriptNamespaces(this.script.id, this.configStore.namespaces);
            await this.scriptService.setReferences(this.script.id, this.configStore.references);
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
