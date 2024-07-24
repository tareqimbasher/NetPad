import {observable} from "@aurelia/runtime";
import {watch} from "@aurelia/runtime-html";
import {IScriptService, ISession, IShortcutManager, Pane, ShortcutIds} from "@application";
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
        this.hasShortcut(shortcutManager.getShortcut(ShortcutIds.openNamespaces));
    }

    public override get name() {
        const environment = this.session.active;
        if (!environment) return this._name;
        return `Namespaces (${environment.script.config.namespaces.length})`;
    }

    public bound() {
        this.activeScriptEnvironmentChanged();
    }

    public async namespacesChanged(newValue: string, oldValue: string) {
        // When the initial value of namespaces is set
        if (oldValue === undefined) {
            return;
        }

        if (newValue === oldValue) {
            return;
        }

        const environment = this.session.active;
        if (!environment) {
            return;
        }

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
        this.updateLocal(this.session.active?.script.config.namespaces);
        this.lastSet = undefined;
    }

    // This is so that the local value does not update while the user is typing, but we still want to react to
    // namespace changes as they can be set by other means (ie. through script properties dialog)
    @watch<NamespacesPane>(vm => vm.session.active?.script.config.namespaces)
    public activeScriptEnvironmentNamespacesChanged() {
        const secondsSinceLastLocalUpdate = !this.lastSet ? null : (new Date().getTime() - this.lastSet?.getTime()) / 1000;

        if (!secondsSinceLastLocalUpdate || secondsSinceLastLocalUpdate >= 2) {
            if (this.session.active)
                this.updateLocal(this.session.active.script.config.namespaces);
        }
    }

    private updateLocal(namespaces: string[] | undefined) {
        this.namespaces = (namespaces ?? []).join("\n") + "\n";
    }
}
