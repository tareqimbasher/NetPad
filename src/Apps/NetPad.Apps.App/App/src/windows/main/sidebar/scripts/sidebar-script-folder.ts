import {SidebarScript} from "./sidebar-script";

export class SidebarScriptFolder {
    constructor(public name: string, public path: string, public parent: SidebarScriptFolder | null) {
    }

    public expanded = false;
    public folders: SidebarScriptFolder[] = [];
    public scripts: SidebarScript[] = [];

    public clone(deep = false): SidebarScriptFolder {
        const clone = new SidebarScriptFolder(this.name, this.path, this.parent);

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
