import {PackageReference, Reference, Script} from "@application";
import {IconName} from "@application/ui/np-icon/icons";

export interface ConfigTab {
    route: string;
    text: string;
    icon: IconName;
    /** Name of the count field on the {@link ConfigStore}, so the rail can bind the count per tab. */
    countProp: "referenceCount" | "packageCount" | "namespaceCount";
}

/**
 * Holds the script's edited configuration while the window is open and which view is showing.
 * Views read and mutate it and the window persists it on Save.
 */
export class ConfigStore {
    public useAspNet: boolean;
    private _script: Script;
    private _namespaces: string[] = [];
    private _references: Reference[] = [];

    public referenceCount = 0;
    public namespaceCount = 0;
    public packageCount = 0;

    public selectedTab: ConfigTab;
    public readonly tabs: ConfigTab[] = [
        {route: "references", text: "References", icon: "references", countProp: "referenceCount"},
        {route: "packages", text: "Packages", icon: "package", countProp: "packageCount"},
        {route: "namespaces", text: "Namespaces", icon: "namespaces", countProp: "namespaceCount"},
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
        this._references = [...script.config.references];
        this.useAspNet = script.config.useAspNet;
        this.updateCounts();
    }

    public updateNamespaces(namespaces: string[]) {
        this._namespaces = [...namespaces];
        this.updateCounts();
    }

    public addNamespace(namespace: string) {
        if (this._namespaces.indexOf(namespace) >= 0) return;
        this._namespaces.push(namespace);
        this.updateCounts();
    }

    public removeNamespace(namespace: string) {
        const ix = this._namespaces.indexOf(namespace);
        if (ix < 0) return;
        this._namespaces.splice(ix, 1);
        this.updateCounts();
    }

    public addReference(reference: Reference) {
        this._references.push(reference);
        this.updateCounts();
    }

    public removeReference(reference: Reference) {
        const ix = this._references.indexOf(reference);
        if (ix < 0) return;
        this._references.splice(ix, 1);
        this.updateCounts();
    }

    private updateCounts() {
        this.referenceCount = this._references.length;
        this.namespaceCount = this._namespaces.length;
        this.packageCount = this._references.filter(r => r instanceof PackageReference).length;
    }
}
