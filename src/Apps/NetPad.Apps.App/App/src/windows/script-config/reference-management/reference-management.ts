import {observable} from "@aurelia/runtime";
import {watch} from "@aurelia/runtime-html";
import * as path from "path";
import Split from "split.js";
import {AssemblyFileReference, IAssemblyService, Reference} from "@application";
import {ConfigStore} from "../config-store";

export class ReferenceManagement {
    @observable public browsedAssemblies: FileList;
    public browseInput: HTMLInputElement;
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
            sizes: [65, 35],
            minSize: [20, 20]
        });
    }

    public get references(): ReadonlyArray<Reference> {
        return this.configStore.references;
    }

    public async selectReference(reference: Reference) {
        this.selectedReference = reference;
        this.namespaces = (await this.assemblyService.getNamespaces(reference))
            .map(ns => new AssemblyNamespace(ns, this.configStore));
    }

    public removeReference(reference: Reference) {
        const ix = this.references.indexOf(reference);
        this.configStore.removeReference(reference);

        if (this.selectedReference && this.references.length) {
            if (ix === this.references.length)
                this.selectedReference = this.references[this.references.length - 1];
            else
                this.selectedReference = this.references[ix];
        } else {
            this.selectedReference = undefined;
        }
    }

    private browsedAssembliesChanged(newValue: FileList) {
        if (!newValue || newValue.length === 0) {
            return;
        }

        const references = Array.from(newValue).map((d: File) => new AssemblyFileReference({
            title: path.basename(d.path),
            assemblyPath: d.path
        }));

        for (const reference of references) {
            this.configStore.addReference(reference);
        }

        // Clear file input element so if user selects X.dll, removes it, then re-selects it
        // the change is observed
        this.browseInput.value = "";
    }

    @watch<ReferenceManagement>(vm => vm.configStore.namespaces.length)
    private updateSelectedWhenNamespacesChange() {
        if (!this.namespaces || this.namespaces.length === 0) return;

        const configured = new Set(this.configStore.namespaces);

        for (const namespace of this.namespaces) {
            namespace.selected = configured.has(namespace.name);
        }
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
        } else {
            this.configStore.addNamespace(this.name);
        }
    }
}
