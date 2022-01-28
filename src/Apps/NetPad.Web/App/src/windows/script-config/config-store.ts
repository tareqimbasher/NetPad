import {Reference} from "@domain";

export class ConfigStore {
    public namespaces: string[] = [];
    public references: Reference[] = [];

    public addNamespace(namespace: string) {
        const ix = this.namespaces.indexOf(namespace);
        if (ix >= 0) return;
        this.namespaces.push(namespace);
    }

    public removeNamespace(namespace: string) {
        const ix = this.namespaces.indexOf(namespace);
        if (ix < 0) return;
        this.namespaces.splice(ix, 1);
    }
}
