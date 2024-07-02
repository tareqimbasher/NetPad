import {Reference, Script} from "@application";

interface ITab {
    route: string;
    text: string;
}

export class ConfigStore {
    public useAspNet: boolean;
    private _script: Script;
    private _namespaces: string[] = [];
    private _references: Reference[] = [];

    public selectedTab: ITab;
    public tabs: ITab[] = [
        {route: "references", text: "References"},
        {route: "packages", text: "Packages"},
        {route: "namespaces", text: "Namespaces"},
    ];

    public get script(): Script {
        return this._script;
    }

    public get namespaces(): ReadonlyArray<string> {
        return this._namespaces;
    }

    public get references(): ReadonlyArray<Reference> {
        return this._references;
    }

    public init(script: Script) {
        this._script = script;
        this._namespaces = [...script.config.namespaces];
        this.updateNamespaceCount();

        this._references = [...script.config.references];
        this.updateReferenceCount();

        this.useAspNet = script.config.useAspNet;
    }

    public updateNamespaces(namespaces: string[]) {
        this._namespaces = [...namespaces];
        this.updateNamespaceCount();
    }

    public addNamespace(namespace: string) {
        const ix = this.namespaces.indexOf(namespace);
        if (ix >= 0) return;
        this._namespaces.push(namespace);
        this.updateNamespaceCount();
    }

    public removeNamespace(namespace: string) {
        const ix = this.namespaces.indexOf(namespace);
        if (ix < 0) return;
        this._namespaces.splice(ix, 1);
        this.updateNamespaceCount();
    }

    private updateNamespaceCount() {
        this.tabs[2].text = `Namespaces (${this.namespaces.length})`;
    }


    public addReference(reference: Reference) {
        this._references.push(reference);
        this.updateReferenceCount();
    }

    public removeReference(reference: Reference) {
        const ix = this._references.indexOf(reference);
        this._references.splice(ix, 1);
        this.updateReferenceCount();
    }

    private updateReferenceCount() {
        this.tabs[0].text = `References (${this.references.length})`;
    }
}
