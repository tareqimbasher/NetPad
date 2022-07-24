import {Pane} from "@application";
import {IScriptService, ISession, IShortcutManager} from "@domain";
import {observable} from "@aurelia/runtime";
import {watch} from "@aurelia/runtime-html";
import {Util} from "@common";

export class NamespacesPane extends Pane {
    @observable namespaces: string;
    private lastSet?: Date;

    constructor(
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager
    ) {
        super("Namespaces", "namespaces-icon");
        this.hasShortcut(shortcutManager.getShortcutByName("Namespaces Pane"));
    }

    public override get name() {
        const environment = this.session.active;
        if (!environment) return this._name;
        return `Namespaces (${environment.script.config.namespaces.length})`;
    }

    public attached() {
        this.activeScriptEnvironmentChanged();
    }

    public async namespacesChanged(newValue: string, oldValue: string) {
        if (newValue === oldValue) return;

        const environment = this.session.active;
        if (!environment) return;

        let namespaces = this.namespaces
            .split('\n')
            .map(ns => ns.trim())
            .filter(ns => ns);

        namespaces = Util.distinct(namespaces);
        this.lastSet = new Date();
        await this.scriptService.setScriptNamespaces(environment.script.id, namespaces);
    }

    @watch<NamespacesPane>(vm => vm.session.active)
    public activeScriptEnvironmentChanged() {
        this.namespaces = this.session.active.script.config.namespaces.join("\n") + "\n";
        this.lastSet = null;
    }

    @watch<NamespacesPane>(vm => vm.session.active.script.config.namespaces)
    public activeScriptEnvironmentNamespacesChanged() {
        const secondsSinceLastLocalUpdate = (new Date().getTime() - this.lastSet?.getTime()) / 1000;

        // This is so that the local value does not update while the user is typing
        if (!this.lastSet || secondsSinceLastLocalUpdate >= 2) {
            this.updateLocal(this.session.active.script.config.namespaces);
        }
    }

    private updateLocal(namespaces: string[]) {
        this.namespaces = namespaces.join("\n") + "\n";
    }
}
