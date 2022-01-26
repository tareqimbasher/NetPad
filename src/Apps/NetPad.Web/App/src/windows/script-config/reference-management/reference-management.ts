import {Reference} from "@domain";
import {ConfigStore} from "../config-store";

export class ReferenceManagement {
    constructor(readonly configStore: ConfigStore) {
        // this.references.push(new PackageReference({
        //     title: "Json.NET",
        //     packageId: "Newtonsoft.Json",
        //     version: "13.0.1.0"
        // }));
        //
        // this.references.push(new AssemblyReference({
        //     title: "Jarvis.dll",
        //     assemblyPath: "/home/tips/Local/Jarvis.dll"
        // }));
    }

    public get references(): Reference[] {
        return this.configStore.references;
    }

    removeReference(reference: Reference) {
        if (!confirm(`Are you sure you want to remove ${reference.title}?`))
            return;

        const ix = this.references.indexOf(reference);
        this.references.splice(ix, 1);
    }
}
