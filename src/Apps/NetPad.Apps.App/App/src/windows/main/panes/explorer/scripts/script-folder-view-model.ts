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

        for (const folder of this.folders) {
            folder.updateStats(expandedFolders);
        }

        this.containingScriptCount = this.calculateScriptCounts();
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

    public findFolders(predicate: (folder: ScriptFolderViewModel) => boolean): ScriptFolderViewModel[] {
        const folders: ScriptFolderViewModel[] = [];

        this.recurseFolders(folder => {
            if (predicate(folder)) {
                folders.push(folder);
            }
        });

        return folders;
    }

    public findFolder(predicate: (f: ScriptFolderViewModel) => boolean): ScriptFolderViewModel | undefined {
        for (const subFolder of this.folders) {
            if (predicate(subFolder)) {
                return subFolder;
            }

            const found = subFolder.findFolder(predicate);
            if (found) {
                return found;
            }
        }
        return undefined;
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
