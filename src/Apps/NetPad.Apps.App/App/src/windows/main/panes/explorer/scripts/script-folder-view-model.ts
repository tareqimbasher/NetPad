import {ScriptViewModel} from "./script-view-model";

export class ScriptFolderViewModel {
    constructor(public name: string, public path: string, public parent: ScriptFolderViewModel | null) {
    }

    public expanded = false;
    public folders: ScriptFolderViewModel[] = [];
    public scripts: ScriptViewModel[] = [];

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
