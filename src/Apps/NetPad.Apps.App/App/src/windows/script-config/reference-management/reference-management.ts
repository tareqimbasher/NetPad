import {observable} from "@aurelia/runtime";
import * as path from "path";
import Split from "split.js";
import {AssemblyReference, IAssemblyService, Reference} from "@domain";
import {ConfigStore} from "../config-store";

export class ReferenceManagement {
    @observable public browsedAssemblies: FileList;
    public selectedReference?: Reference;
    public namespaces: AssemblyNamespace[] = [];

    constructor(
        readonly configStore: ConfigStore,
        @IAssemblyService readonly assemblyService: IAssemblyService
    ) {
    }

    public attached() {
        Split(["#references-list", "#namespace-selection"], {
            gutterSize: 6,
            sizes: [80, 20],
            minSize: [50, 50]
        });
    }

    public get references(): Reference[] {
        return this.configStore.references;
    }

    public async selectReference(reference: Reference) {
        this.selectedReference = reference;
        this.namespaces = (await this.assemblyService.getNamespaces(reference))
            .map(ns => new AssemblyNamespace(ns, this.configStore));
    }

    public removeReference(reference: Reference) {
        const ix = this.references.indexOf(reference);
        this.references.splice(ix, 1);
        this.selectedReference = null;
    }

    private browsedAssembliesChanged(newValue: FileList) {
        if (!newValue || newValue.length === 0)
            return;

        const references = Array.from(newValue).map((d: File | any) => new AssemblyReference({
            title: path.basename(d.path),
            assemblyPath: d.path
        }));

        this.configStore.references.push(...references);
    }
}

class AssemblyNamespace {
    @observable public selected: boolean;

    constructor(
        public name: string,
        public configStore: ConfigStore
    ) {
        this.selected = this.configStore.namespaces.indexOf(this.name) >= 0;
    }

    public selectedChanged(newValue: boolean, oldValue: boolean) {
        if (newValue === oldValue) return;

        if (!newValue) {
            this.configStore.removeNamespace(this.name);
        }
        else {
            this.configStore.addNamespace(this.name);
        }
    }
}
