import {CancellationToken, editor, languages} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

export async function findUsages(
    model: editor.ITextModel,
    omnisharpService: IOmniSharpService,
    lineNumber: number,
    column: number,
    excludeDefinition: boolean,
    token: CancellationToken): Promise<languages.Location[] | null> {

    const scriptId = EditorUtil.getScriptId(model);

    const response = await omnisharpService.findUsages(scriptId, new api.FindUsagesRequest({
        onlyThisFile: true,
        excludeDefinition: excludeDefinition,
        line: lineNumber,
        column: column,
        applyChangesTogether: false
    }), new AbortController().signalFrom(token));

    if (!response || !response.quickFixes) {
        return null;
    }

    return response.quickFixes.map(qf => {
        return {
            uri: model.uri,
            range: Converter.apiQuickFixToMonacoRange(qf)
        }
    });
}
