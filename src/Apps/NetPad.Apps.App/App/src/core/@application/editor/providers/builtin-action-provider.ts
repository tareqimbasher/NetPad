import * as monaco from "monaco-editor";
import {
    ApiException,
    IActionProvider,
    ISession,
    MonacoEditorUtil,
    RecentScriptsStore,
    ScriptEnvironment,
    ScriptKind,
    ScriptsStore,
    ScriptSummary
} from "@application";

export class BuiltinActionProvider implements IActionProvider {
    constructor(
        @ISession private readonly session: ISession,
        private readonly scriptsStore: ScriptsStore,
        private readonly recentScriptsStore: RecentScriptsStore) {
    }

    public provideActions(): monaco.editor.IActionDescriptor[] {
        return [
            {
                id: "netpad.action.transformToUpperOrLowercase",
                label: "Transform to Upper/Lower Case",
                keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyY],
                run: (editor) => {
                    const model = editor?.getModel();
                    const currentSelection = editor?.getSelection();

                    if (!editor || !model || !currentSelection) return;

                    const selectedValue = model.getValueInRange(currentSelection);

                    if (!selectedValue.trim()) return;

                    if (selectedValue === selectedValue.toLowerCase()) {
                        editor.trigger(null, "editor.action.transformToUppercase", null);
                    } else {
                        editor.trigger(null, "editor.action.transformToLowercase", null);
                    }
                }
            },
            {
                id: "netpad.action.goToScript",
                label: "Go to Script",
                keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyT],
                run: () => {
                    const picks = this.buildPicks(
                        [...this.session.environments],
                        this.scriptsStore.scripts,
                        this.recentScriptsStore.recentScripts,
                        this.session.active?.script.id
                    );

                    // matchOnDescription lets typing filter by path/folder, not just the script name.
                    MonacoEditorUtil.getQuickInputService()
                        .pick(picks, {placeholder: "Go to script", matchOnDescription: true})
                        .then((selected: ScriptPick | undefined) => {
                            if (selected) {
                                this.open(selected.script);
                            }
                        });
                }
            }
        ];
    }

    /**
     * Builds the grouped quick-pick list: open scripts (active first), then recents (excluding open
     * ones), then the full library (excluding open ones). A recent that is also a saved script is
     * rendered with that script's name/kind and still appears under "Library"; a recent that is
     * neither open nor in the library is shown by file name and opened by path.
     */
    private buildPicks(
        opened: readonly ScriptEnvironment[],
        libraryScripts: readonly ScriptSummary[],
        recentScripts: readonly string[],
        activeScriptId: string | undefined
    ): GoToScriptPick[] {
        const picks: GoToScriptPick[] = [];

        const openEnvIds = new Set(opened.map(e => e.script.id));
        const openEnvPaths = new Set(
            opened.map(e => e.script.path).filter(p => !!p)
        );

        // Open scripts (active first)
        if (opened.length) {
            picks.push({type: "separator", label: "Open"});
            picks.push(...opened
                .map(env => this.toPick({
                    id: env.script.id,
                    name: env.script.name,
                    kind: env.script.config.kind,
                    path: env.script.path,
                    isDirty: env.script.isDirty
                }))
                .sort((a, b) => Number(b.id === activeScriptId) - Number(a.id === activeScriptId))
            );
        }

        // Index library scripts by path so a recent that is also a saved script renders with its real
        // name/kind (it still appears under "Library" too)
        const libScriptsByPath = new Map<string, ScriptSummary>();
        for (const s of libraryScripts) {
            if (s.path) libScriptsByPath.set(s.path, s);
        }

        // Recents (most-recent-first), excluding any that are already open
        const recents = recentScripts.filter(path => !openEnvPaths.has(path));
        if (recents.length) {
            picks.push({type: "separator", label: "Recent"});
            picks.push(...recents.map(path => {
                const inLib = libScriptsByPath.get(path);
                return inLib ? this.toPick(inLib) : this.toRecentPick(path);
            }));
        }

        // All library scripts, excluding those already open
        const libraryPicks = libraryScripts
            .filter(s => !openEnvIds.has(s.id))
            .map(script => this.toPick(script))
            .sort((a, b) => a.label > b.label ? 1 : -1);
        if (libraryPicks.length) {
            picks.push({type: "separator", label: "Library"});
            picks.push(...libraryPicks);
        }

        return picks;
    }

    private open(script: ScriptRef) {
        if (script.path) {
            const path = script.path;
            this.session.openByPath(path).catch(err => {
                // A recents-only entry (no saved-script id) that no longer exists on disk: prune it so
                // it stops surfacing. Other failures are transient and left in place.
                if (!script.id && err instanceof ApiException && err.status === 404) {
                    this.recentScriptsStore.remove(path).catch(() => undefined);
                }
            });
        } else if (script.id) {
            this.session.activate(script.id);
        }
    }

    private toPick(script: {
        id: string;
        name: string;
        kind: ScriptKind;
        path?: string;
        isDirty?: boolean
    }): ScriptPick {
        const icon = script.kind === "SQL" ? "$(sql)" : "$(csharp)";

        return {
            type: "item",
            id: script.id,
            label: `${icon} ${script.name}`,
            description: !script.path ? "(New)" : ((script.isDirty ? "(Unsaved) " : "") + script.path),
            script
        };
    }

    /**
     * Builds a pick for a recent script known only by its file path.
     * Kind is unknown, so a neutral file icon is used. Selecting it opens by path.
     */
    private toRecentPick(path: string): ScriptPick {
        const fileName = this.fileName(path).replace(/\.netpad$/i, "");
        return {
            type: "item",
            id: `recent:${path}`,
            label: `$(file) ${fileName}`,
            description: path,
            script: {path}
        };
    }

    private fileName(path: string): string {
        const normalized = path.replace(/\\/g, "/");
        return normalized.substring(normalized.lastIndexOf("/") + 1) || path;
    }
}

interface ScriptRef {
    id?: string;
    path?: string;
}

interface ScriptPick {
    type: "item";
    id: string;
    label: string;
    description: string;
    script: ScriptRef;
}

interface ScriptSeparator {
    type: "separator";
    label: string;
}

type GoToScriptPick = ScriptPick | ScriptSeparator;
