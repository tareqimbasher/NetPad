import {Reference} from "@domain";

export class ConfigStore {
    public selectedTab;
    public tabs = [
        {route: "references", text: "References"},
        {route: "packages", text: "Packages"},
        {route: "namespaces", text: "Namespaces"},
    ];

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
