import {PackageReference, Reference, Script} from "@application";
import {RoutedPages} from "@application/windowing/routed-pages";
import {RailNavPage} from "@application/ui/rail-nav/np-rail-nav";

export interface ConfigPage extends RailNavPage {
    count: number;
}

/**
 * Holds the script's edited configuration while the window is open and which view is showing.
 * Views read and mutate it and the window persists it on Save.
 */
export class ConfigStore extends RoutedPages<ConfigPage> {
    public readonly identityParams = ["script-id"];

    public useAspNet: boolean;
    private _script: Script;
    private _namespaces: string[] = [];
    private _references: Reference[] = [];

    constructor() {
        super([
            {route: "references", text: "References", icon: "references", count: 0},
            {route: "packages", text: "Packages", icon: "package", count: 0},
            {route: "namespaces", text: "Namespaces", icon: "namespaces", count: 0},
        ]);
    }

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
        const counts: Record<string, number> = {
            references: this._references.length,
            packages: this._references.filter(r => r instanceof PackageReference).length,
            namespaces: this._namespaces.length,
        };

        for (const page of this.pages) {
            page.count = counts[page.route];
        }
    }
}
