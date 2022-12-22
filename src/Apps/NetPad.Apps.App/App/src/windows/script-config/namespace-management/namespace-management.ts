import {observable} from "@aurelia/runtime";
import {watch} from "@aurelia/runtime-html";
import {ConfigStore} from "../config-store";
import {Util} from "@common";

export class NamespaceManagement {
    @observable namespaces: string;
    public error?: string;
    private lastSet?: Date;

    constructor(readonly configStore: ConfigStore) {
    }

    public binding() {
        this.configStoreChanged();
    }

    public namespacesChanged(newValue: string) {
        const namespaces = new Set<string>();

        for (const namespace of this.namespaces.split("\n")) {
            if (!namespace) continue;

            const error = this.validate(namespace);

            if (error) {
                this.error = error;
                return;
            }

            namespaces.add(namespace.trim());
        }

        this.error = undefined;
        this.lastSet = new Date();

        this.configStore.updateNamespaces([...namespaces]);
    }

    @watch<NamespaceManagement>(vm => vm.configStore.namespaces.length)
    public configStoreChanged() {
        const secondsSinceLastLocalUpdate = !this.lastSet ? null : (new Date().getTime() - this.lastSet?.getTime()) / 1000;

        // To stop an infinite loop
        if (!secondsSinceLastLocalUpdate || secondsSinceLastLocalUpdate >= 1) {
            this.updateLocal(this.configStore.namespaces);
        }
    }

    private updateLocal(namespaces: string[] | ReadonlyArray<string>) {
        this.namespaces = this.configStore.namespaces.join("\n") + "\n";
    }

    private validate(namespace: string): string | null {
        if (namespace.endsWith(";"))
            return `The namespace "${namespace}" should not end with a semi-colon ";"`;
        if (namespace.startsWith("using "))
            return `The namespace "${namespace}" should not start with "using"`;
        if (namespace.length > 0 && !Util.isLetter(namespace[0]) && namespace[0] !== "_")
            return `The namespace "${namespace}" seems incorrect. It must start with an alphabet or underscore`;

        return null;
    }
}
