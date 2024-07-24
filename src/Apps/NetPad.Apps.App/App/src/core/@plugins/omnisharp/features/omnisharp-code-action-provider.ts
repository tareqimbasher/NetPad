import {CancellationToken, editor, languages, Range} from "monaco-editor";
import {ICodeActionProvider, ICommandProvider, IScriptService, ISession, MonacoEditorUtil} from "@application";
import {Converter, TextChangeUtil} from "../utils";
import * as api from "../api";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpCodeActionProvider extends FeatureProvider implements ICodeActionProvider, ICommandProvider {
    private readonly commandId = "netpad.command.omnisharp.runCodeAction";
    private readonly excludedCodeActionIdentifiers: (string | ((str: string) => boolean))[] = [
        "Convert_to_Program_Main_style_program",
        "Remove Unnecessary Usings",
        id => id.indexOf("in new file") >= 0,
        id => id.indexOf("Move type ") >= 0,
        id => id.indexOf("Rename file ") >= 0,
    ];

    constructor(
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService) {
        super();
    }

    public provideCommands(): { id: string; handler: (accessor: unknown, ...args: unknown[]) => void; }[] {
        return [{
            id: this.commandId,
            handler: (accessor: unknown, ...args: unknown[]) => {
                return this.runCodeAction(args[0] as string, args[1] as editor.ITextModel, args[2] as api.RunCodeActionRequest);
            }
        }];
    }

    public async provideCodeActions(model: editor.ITextModel, range: Range, context: languages.CodeActionContext, token: CancellationToken): Promise<languages.CodeActionList> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const request = new api.GetCodeActionsRequest({
            line: range.startLineNumber,
            column: range.startColumn,
            applyChangesTogether: false,
            selection: !range ? undefined : Converter.monacoRangeToApiRange(range)
        });

        const response = await this.omnisharpService.getCodeActions(scriptId, request, this.getAbortSignal(token));

        if (!response || !response.codeActions) {
            return {
                actions: [],
                dispose: () => {
                    // do nothing
                }
            }
        }

        const codeActions: languages.CodeAction[] = [];

        for (const codeAction of this.filterCodeActions(response.codeActions)) {
            const runRequest = new api.RunCodeActionRequest({
                identifier: codeAction.identifier,
                line: request.line,
                column: request.column,
                applyChangesTogether: request.applyChangesTogether,
                selection: request.selection,
                applyTextChanges: false,
                wantsTextChanges: true,
                wantsAllCodeActionOperations: false
            });

            const codeActionName = codeAction.name || "(no name)";

            codeActions.push({
                title: codeActionName,
                kind: this.convertToMonacoCodeCodeActionKind(codeAction.codeActionKind),
                command: {
                    id: this.commandId,
                    title: codeActionName,
                    arguments: [scriptId, model, runRequest]
                }
            });
        }

        return {
            actions: codeActions,
            dispose: () => {
                // do nothing
            }
        }
    }

    private convertToMonacoCodeCodeActionKind(kind: string | undefined): string | undefined {
        switch (kind) {
            case "QuickFix":
                return "quickfix";
            case "Refactor":
                return "refactor";
            case "RefactorExtract":
                return "refactor.extract";
            case "RefactorInline":
                return "refactor.inline";
            default:
                return undefined;
        }
    }

    private async runCodeAction(scriptId: string, model: editor.ITextModel, runRequest: api.RunCodeActionRequest) {

        const versionBeforeRequest = model.getVersionId();

        const response = await this.omnisharpService.runCodeAction(scriptId, runRequest, MonacoEditorUtil.abortSignalFrom(10000));

        if (!response || !response.changes || versionBeforeRequest !== model.getVersionId()) {
            return;
        }

        const modifications: api.LinePositionSpanTextChange[] = [];

        for (const change of response.changes) {
            if (change.modificationType === "Modified") {
                modifications.push(...(change as api.ModifiedFileResponse).changes);
            } else if (change.modificationType === "Renamed") {
                console.warn("Not handling Rename modification types")
            } else if (change.modificationType === "Opened") {
                console.warn("Not handling Open modification types")
            }
        }

        if (modifications.length) {
            await TextChangeUtil.applyTextChanges(model, modifications, this.session, this.scriptService);
        }
    }

    private filterCodeActions(actions: api.OmniSharpCodeAction[]): api.OmniSharpCodeAction[] {
        return actions.filter(a => {
            if (!a.identifier)
                return true;

            for (const excludedCodeActionIdentifier of this.excludedCodeActionIdentifiers) {
                if (typeof excludedCodeActionIdentifier === "string") {
                    if (a.identifier.indexOf(excludedCodeActionIdentifier) >= 0)
                        return false;
                } else if (excludedCodeActionIdentifier(a.identifier))
                    return false;
            }

            return true;
        });
    }
}
