import {EditorActionPaletteSource} from "@application/command-palette/sources/editor-action-palette-source";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {ITextEditorService} from "@application/editor/itext-editor-service";

const editorId = "editor-1";

function createSource(actions: { id: string; label: string }[], wrapped: string[]) {
    const editor = {
        getId: () => editorId,
        getSupportedActions: () => actions.map(action => ({...action, run: () => undefined})),
        focus: () => undefined,
    };

    const textEditorService = {active: {monaco: editor}} as unknown as ITextEditorService;
    const commandRegistry = {
        commands: wrapped.map(monacoCommandId => ({monacoCommandId})),
    } as unknown as ICommandRegistry;

    return new EditorActionPaletteSource(textEditorService, commandRegistry);
}

function titles(source: EditorActionPaletteSource): string[] {
    return source.getGroups().flatMap(group => group.items).map(item => item.title);
}

describe("EditorActionPaletteSource", () => {
    test("an action a command wraps is not federated a second time", () => {
        const source = createSource(
            [
                {id: "editor.action.selectAll", label: "Select All"},
                {id: "editor.action.formatDocument", label: "Format Document"},
            ],
            ["editor.action.selectAll"]
        );

        expect(titles(source)).toEqual(["Format Document"]);
    });

    test("a contributed action is deduped despite the editor-scoped id it is listed under", () => {
        const source = createSource(
            [
                {id: `${editorId}:netpad.action.transformToUpperOrLowercase`, label: "Transform to Upper/Lower Case"},
                {id: `${editorId}:netpad.action.somethingElse`, label: "Something Else"},
            ],
            ["netpad.action.transformToUpperOrLowercase"]
        );

        expect(titles(source)).toEqual(["Something Else"]);
    });

    test("the editor's own command list is left out — it is the surface the palette replaces", () => {
        const source = createSource(
            [
                {id: "editor.action.quickCommand", label: "Command Palette"},
                {id: "editor.action.gotoLine", label: "Go to Line/Column..."},
            ],
            []
        );

        expect(titles(source)).toEqual(["Go to Line/Column..."]);
    });

    test("an action without a label is not offered", () => {
        const source = createSource([{id: "editor.action.nameless", label: ""}], []);

        expect(source.getGroups()).toEqual([]);
    });

    test("no editor means no Editor section", () => {
        const source = new EditorActionPaletteSource(
            {active: undefined} as unknown as ITextEditorService,
            {commands: []} as unknown as ICommandRegistry
        );

        expect(source.getGroups()).toEqual([]);
    });
});
