import {Util} from "@common";
import {ApiException, ScriptKind, ScriptSummary, Settings} from "@application/api";
import {ISession} from "@application/sessions/isession";
import {RecentScriptsStore} from "@application/sessions/recent-scripts-store";
import {ScriptsStore} from "@application/scripts/scripts-store";
import {scriptKindBadge} from "@application/scripts/script-kind-badge";
import {IPaletteSource} from "../ipalette-source";
import {PaletteMode} from "../palette-grammar";
import {PaletteGroup, PaletteItem} from "../palette-item";

interface ScriptRef {
    id?: string;
    path?: string;
}

/** The scripts a user can go to. */
export class ScriptPaletteSource implements IPaletteSource {
    public readonly mode = PaletteMode.Scripts;
    public readonly order = 0;

    constructor(
        @ISession private readonly session: ISession,
        private readonly settings: Settings,
        private readonly scriptsStore: ScriptsStore,
        private readonly recentScriptsStore: RecentScriptsStore) {
    }

    public getGroups(): PaletteGroup[] {
        const groups: PaletteGroup[] = [];

        const opened = [...this.session.environments];
        const activeScriptId = this.session.active?.script.id;
        const openIds = new Set(opened.map(e => e.script.id));
        const openPaths = new Set(opened.map(e => e.script.path).filter(p => !!p));

        // Show open scripts first, active one first.
        if (opened.length) {
            groups.push({
                label: "Open",
                items: opened
                    .sort((a, b) =>
                        Number(b.script.id === activeScriptId) - Number(a.script.id === activeScriptId))
                    .map(env => this.toItem({
                        id: env.script.id,
                        name: env.script.name,
                        kind: env.script.config.kind,
                        path: env.script.path,
                        isDirty: env.script.isDirty,
                    })),
            });
        }

        // A recent that is also a saved script renders with that script's name and kind.
        const libraryByPath = new Map<string, ScriptSummary>();
        for (const script of this.scriptsStore.scripts) {
            if (script.path) libraryByPath.set(script.path, script);
        }

        const recents = this.recentScriptsStore.recentScripts.filter(path => !openPaths.has(path));
        if (recents.length) {
            groups.push({
                label: "Recent",
                items: recents.map(path => {
                    const inLibrary = libraryByPath.get(path);
                    return inLibrary ? this.toItem(inLibrary, "recent") : this.toPathItem(path);
                }),
            });
        }

        const library = this.scriptsStore.scripts
            .filter(script => !openIds.has(script.id))
            .map(script => this.toItem(script))
            .sort((a, b) => a.title > b.title ? 1 : -1);
        if (library.length) {
            groups.push({label: "Library", items: library});
        }

        return groups;
    }

    private toItem(
        script: { id: string; name: string; kind: ScriptKind; path?: string; isDirty?: boolean },
        idPrefix = "script"
    ): PaletteItem {
        return {
            id: `${idPrefix}:${script.id}`,
            title: script.name,
            badge: scriptKindBadge(script.kind) ?? "",
            dirty: script.isDirty,
            detail: script.path ? this.describeLocation(script.path) : "new",
            run: () => this.open(script),
        };
    }

    /** A recent known only by its file path has an unknown kind and it opens by path. */
    private toPathItem(path: string): PaletteItem {
        const normalized = path.replaceAll("\\", "/");
        const fileName = normalized.substring(normalized.lastIndexOf("/") + 1) || path;

        return {
            id: `recent:${path}`,
            title: fileName.replace(/\.netpad$/i, ""),
            badge: "",
            detail: normalized,
            run: () => this.open({path}),
        };
    }

    /**
     * Where a script lives. For a script in the script library, its path relative to the script library.
     * And for a script that lives outside the script library, its full path.
     */
    private describeLocation(path: string): string {
        const normalized = path.replaceAll("\\", "/");
        const library = Util.trimEnd(this.settings.scriptsDirectoryPath.replaceAll("\\", "/"), "/");

        if (!library || !normalized.startsWith(library + "/")) return normalized;

        const libraryName = library.substring(library.lastIndexOf("/") + 1);
        const relative = normalized.substring(library.length + 1);
        const folder = relative.substring(0, relative.lastIndexOf("/"));

        return folder ? `${libraryName}/${folder}` : libraryName;
    }

    private open(script: ScriptRef) {
        if (script.path) {
            const path = script.path;
            return this.session.openByPath(path).catch(err => {
                // A recents-only entry (no saved-script id) that no longer exists on disk: prune it
                // so it stops surfacing.
                if (!script.id && err instanceof ApiException && err.status === 404) {
                    this.recentScriptsStore.remove(path).catch(() => undefined);
                }
            });
        }

        if (script.id) return this.session.activate(script.id);

        return undefined;
    }
}
