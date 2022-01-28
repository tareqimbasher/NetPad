import {AssemblyReference, Reference} from "@domain";
import {ConfigStore} from "../config-store";
import {observable} from "@aurelia/runtime";
import * as path from "path";

export class ReferenceManagement {
    @observable public browsedAssemblies: FileList;

    constructor(readonly configStore: ConfigStore) {
    }

    public get references(): Reference[] {
        return this.configStore.references;
    }

    public removeReference(reference: Reference) {
        const ix = this.references.indexOf(reference);
        this.references.splice(ix, 1);
    }

    private browsedAssembliesChanged(newValue: FileList) {
        if (!newValue || newValue.length === 0)
            return;

        const references = Array.from(newValue).map(d => new AssemblyReference({
            title: path.basename(d.path),
            assemblyPath: d.path
        }));

        this.configStore.references.push(...references);
    }
}
