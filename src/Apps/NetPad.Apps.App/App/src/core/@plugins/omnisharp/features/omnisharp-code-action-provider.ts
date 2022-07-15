import {CancellationToken, editor, languages, Range} from "monaco-editor";
import {IScriptService, ISession} from "@domain";
import {EditorUtil, ICommandProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {
    GetCodeActionsRequest, LinePositionSpanTextChange,
    ModifiedFileResponse,
    Point,
    Range as OmniSharpRange,
    RunCodeActionRequest
} from "../api";
import {Util} from "@common";

export class OmnisharpCodeActionProvider implements languages.CodeActionProvider, ICommandProvider {
    private readonly commandId = "omnisharp.runCodeAction";
    private readonly excludedCodeActionIdentifiers = [
        "Convert_to_Program_Main_style_program",
        "Remove Unnecessary Usings"
    ];

    constructor(
        @IOmniSharpService private readonly omnisharpService: IOmniSharpService,
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService) {
    }

    public provideCommands(): { id: string; handler: (accessor: any, ...args: any[]) => void; }[] {
        return [{
            id: this.commandId,
            handler: (accessor: any, ...args: any[]) => {
                return this.runCodeAction(args[0], args[1], args[2]);
            }
        }];
    }

    public async provideCodeActions(model: editor.ITextModel, range: Range, context: languages.CodeActionContext, token: CancellationToken): Promise<languages.CodeActionList> {
        const scriptId = EditorUtil.getScriptId(model);

        const request = new GetCodeActionsRequest({
            line: range.startLineNumber,
            column: range.startColumn,
            applyChangesTogether: false,
            selection: !range ? null : new OmniSharpRange({
                start: new Point({
                    line: range.startLineNumber,
                    column: range.startColumn
                }),
                end: new Point({
                    line: range.endLineNumber,
                    column: range.endColumn
                })
            })
        });

        const response = await this.omnisharpService.getCodeActions(scriptId, request);

        if (!response || !response.codeActions) {
            return {
                actions: [],
                dispose: () => {
                }
            }
        }

        const codeActions: languages.CodeAction[] = [];

        for (const codeAction of response.codeActions) {
            if (this.excludedCodeActionIdentifiers.indexOf(codeAction.identifier) >= 0) {
                continue;
            }

            const runRequest = new RunCodeActionRequest({
                identifier: codeAction.identifier,
                line: request.line,
                column: request.column,
                applyChangesTogether: request.applyChangesTogether,
                selection: request.selection,
                applyTextChanges: false,
                wantsTextChanges: true,
                wantsAllCodeActionOperations: false
            });

            codeActions.push({
                title: codeAction.name,
                command: {
                    id: this.commandId,
                    title: codeAction.name,
                    arguments: [scriptId, model, runRequest]
                }
            });
        }

        return {
            actions: codeActions,
            dispose: () => {
            }
        }
    }

    private async runCodeAction(scriptId: string, model: editor.ITextModel, runRequest: RunCodeActionRequest) {

        const versionBeforeRequest = model.getVersionId();

        const response = await this.omnisharpService.runCodeAction(scriptId, runRequest);

        if (!response || !response.changes || versionBeforeRequest !== model.getVersionId()) {
            return true;
        }

        const editorLineCount = model.getLineCount();
        const edits: editor.IIdentifiedSingleEditOperation[] = [];

        for (const change of response.changes) {
            if (change.modificationType === "Modified") {
                const modifiedFileResponse = change as ModifiedFileResponse;

                for (const textChange of modifiedFileResponse.changes) {
                    textChange.endColumn++; // OmniSharp sends this back short one char

                    const isOutOfEditorRange = (textChange.startLine < 1 && textChange.endLine < 1)
                        || (textChange.startLine > editorLineCount)

                    if (isOutOfEditorRange) {
                        await this.processOutOfEditorRangeTextChange(scriptId, textChange);
                        continue;
                    }

                    if (textChange.startLine < 1) {
                        // Discard text changes that occur before the first line
                        textChange.newText = textChange.newText.split("\n")
                            .slice(1 - textChange.startLine)
                            .join("\n");

                        textChange.startLine = 1;
                    }

                    edits.push({
                        text: textChange.newText,
                        range: {
                            startLineNumber: textChange.startLine,
                            startColumn: textChange.startColumn,
                            endLineNumber: textChange.endLine,
                            endColumn: textChange.endColumn
                        },
                        forceMoveMarkers: false
                    });
                }
            }
            else if (change.modificationType === "Renamed") {
                console.warn("Not handling Rename modification types")
            }
            else if (change.modificationType === "Opened") {
                console.warn("Not handling Open modification types")
            }
        }

        if (edits.length > 0) {
            // Using this instead of 'model.applyEdits()' so that undo stack is preserved
            model.pushEditOperations([], edits, () => []);
            model.pushStackElement();
        }
    }

    private async processOutOfEditorRangeTextChange(scriptId: string, textChange: LinePositionSpanTextChange) {
        const newLines = textChange.newText.split("\n")
            .map(l => l.trim())
            .filter(l => l);

        if (newLines.filter(l => l.startsWith("using ")).length == newLines.length) {
            const environment = await this.session.environments.find(e => e.script.id === scriptId);
            if (environment) {
                const namespaces = new Set<string>([...environment.script.config.namespaces]);

                for (const newLine of newLines) {
                    let namespace = newLine.slice("using ".length - 1);
                    namespace = Util.trimEnd(namespace, ";").trim();
                    namespaces.add(namespace);
                }

                await this.scriptService.setScriptNamespaces(scriptId, [...namespaces]);
            }
        }
    }
}
