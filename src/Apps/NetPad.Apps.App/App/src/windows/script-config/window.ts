import {IScriptService, ISession, IWindowService, Reference, Script} from "@application";
import {ConfigStore} from "./config-store";
import {WindowBase} from "@application/windows/window-base";
import {WindowParams} from "@application/windows/window-params";

export class Window extends WindowBase {
    public script: Script;

    constructor(
        private readonly windowParams: WindowParams,
        private readonly configStore: ConfigStore,
        @ISession private readonly session: ISession,
        @IWindowService private readonly windowService: IWindowService,
        @IScriptService private readonly scriptService: IScriptService) {
        super();

        let tabIndex = this.configStore.tabs.findIndex(t => t.route === this.windowParams.get("tab"));
        if (tabIndex < 0)
            tabIndex = 0;

        this.configStore.selectedTab = this.configStore.tabs[tabIndex];
    }

    public async binding() {
        const scriptId = this.windowParams.get("script-id");
        if (!scriptId) throw new Error("No script ID provided");

        const environment = await this.session.getEnvironment(scriptId);
        this.script = environment.script;

        document.title = `${this.script.name} - Properties`;

        this.configStore.init(this.script);
    }

    public async save() {
        try {
            await this.scriptService.setScriptNamespaces(this.script.id, this.configStore.namespaces as string[]);
            await this.scriptService.setReferences(this.script.id, this.configStore.references as Reference[]);
            await this.scriptService.setUseAspNet(this.script.id, this.configStore.useAspNet);

            await this.windowService.close();
        } catch (ex) {
            alert(ex);
        }
    }

    public async cancel() {
        await this.windowService.close();
    }
}
