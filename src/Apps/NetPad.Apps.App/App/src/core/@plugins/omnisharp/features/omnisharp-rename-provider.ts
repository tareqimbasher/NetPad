import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {MonacoEditorUtil, IRenameProvider} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";

export class OmnisharpRenameProvider extends FeatureProvider implements IRenameProvider {
    public async provideRenameEdits(model: editor.ITextModel, position: Position, newName: string, token: CancellationToken): Promise<languages.WorkspaceEdit & languages.Rejection | undefined> {
        const scriptId = MonacoEditorUtil.getScriptId(model);
        const versionIdBeforeRequest = model.getVersionId();

        const response = await this.omnisharpService.rename(scriptId, new api.RenameRequest({
            line: position.lineNumber,
            column: position.column,
            renameTo: newName,
            applyChangesTogether: false,
            applyTextChanges: false,
            wantsTextChanges: true
        }), this.getAbortSignal(token));

        if (!response || !response.changes || response.errorMessage) {
            return {
                edits: [],
                rejectReason: response.errorMessage
            };
        }

        const textEdits: languages.IWorkspaceTextEdit[] = [];

        for (const changeGroup of response.changes) {
            for (const change of changeGroup.changes) {
                const textEdit: languages.IWorkspaceTextEdit = {
                    resource: model.uri,
                    versionId: versionIdBeforeRequest,
                    textEdit: Converter.apiLinePositionSpanTextChangeToMonacoTextEdit(change)
                };

                textEdits.push(textEdit);
            }
        }

        return {
            edits: textEdits
        };
    }
}
