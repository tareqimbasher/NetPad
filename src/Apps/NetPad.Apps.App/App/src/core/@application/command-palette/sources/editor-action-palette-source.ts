import {ICommandRegistry} from "@application/commands/icommand-registry";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {MonacoEditorUtil} from "@application/editor/monaco/monaco-editor-util";
import {IPaletteSource} from "../ipalette-source";
import {PaletteMode} from "../palette-grammar";
import {PaletteGroup, PaletteItem} from "../palette-item";

/**
 * The active editor's own actions, minus those an app command already wraps.
 */
export class EditorActionPaletteSource implements IPaletteSource {
    public readonly mode = PaletteMode.Commands;
    public readonly order = 1;

    /** Monaco's own command palette, which this palette replaces. */
    private readonly excluded = new Set(["editor.action.quickCommand"]);

    constructor(
        @ITextEditorService private readonly textEditorService: ITextEditorService,
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry) {
    }

    public getGroups(): PaletteGroup[] {
        const editor = this.textEditorService.active?.monaco;
        if (!editor) return [];

        const wrapped = new Set(
            this.commandRegistry.commands
                .map(command => command.monacoCommandId)
                .filter((id): id is string => !!id)
        );

        // An action contributed to an editor has the editor's id as a prefix. Strip it, or the action never
        // matches the command that wraps it.
        const scope = `${editor.getId()}:`;
        const identify = (actionId: string) => actionId.startsWith(scope) ? actionId.substring(scope.length) : actionId;

        const items: PaletteItem[] = editor.getSupportedActions()
            .filter(action => !!action.label
                && !wrapped.has(identify(action.id))
                && !this.excluded.has(identify(action.id)))
            .map(action => ({
                id: action.id,
                title: action.label,
                keys: MonacoEditorUtil.getKeybindingCaps(action.id),
                run: () => {
                    editor.focus();
                    return action.run();
                },
            }));

        return items.length ? [{label: "Editor", items}] : [];
    }
}
