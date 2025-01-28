import {ScriptViewModel} from "./script-view-model";

export class ScriptFolderViewModel {
    constructor(public name: string, public path: string, public parent: ScriptFolderViewModel | null) {
    }

    public expanded = false;
    public folders: ScriptFolderViewModel[] = [];
    public scripts: ScriptViewModel[] = [];
    public containingScriptCount: number = 0;

    public updateStats(expandedFolders: Set<string>) {
        if (expandedFolders.has(this.path)) {
            this.expanded = true;
        }

        this.containingScriptCount = this.calculateScriptCounts();

        this.recurseFolders(folder => {
            this.containingScriptCount += folder.scripts.length;
            folder.updateStats(expandedFolders);
        });
    }

    public calculateScriptCounts(): number {
        let scriptCount = this.scripts.length;

        for (const subfolder of this.folders) {
            scriptCount += subfolder.calculateScriptCounts();
        }

        this.containingScriptCount = scriptCount;

        return scriptCount;
    }

    public recurseFolders(func: (f: ScriptFolderViewModel) => void) {
        for (const subFolder of this.folders) {
            subFolder.recurseFolders(func);
            func(subFolder);
        }
    }

    public clone(deep = false): ScriptFolderViewModel {
        const clone = new ScriptFolderViewModel(this.name, this.path, this.parent);

        clone.expanded = this.expanded;

        if (deep) {
            for (const folder of this.folders) {
                clone.folders.push(folder.clone(deep));
            }
            for (const script of this.scripts) {
                clone.scripts.push(script);
            }
        }

        return clone;
    }
}
