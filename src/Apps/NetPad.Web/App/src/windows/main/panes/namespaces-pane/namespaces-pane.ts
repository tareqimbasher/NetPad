import {Pane} from "@application";
import {IScriptService, ISession} from "@domain";
import {observable} from "@aurelia/runtime";
import {watch} from "aurelia";
import {Util} from "@common";

export class NamespacesPane extends Pane {
    @observable namespaces: string;

    constructor(@ISession readonly session: ISession, @IScriptService readonly scriptService: IScriptService) {
        super("Namespaces", "list");
    }

    public get name() {
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
            .map(ns => ns.trim());

        namespaces = Util.distinct(namespaces);
        await this.scriptService.setScriptNamespaces(environment.script.id, namespaces);
    }

    @watch<NamespacesPane>(vm => vm.session.active)
    @watch<NamespacesPane>(vm => vm.session.active.script.config.namespaces.length)
    public activeScriptEnvironmentChanged() {
        this.namespaces = this.session.active.script.config.namespaces.join('\n');
    }
}
