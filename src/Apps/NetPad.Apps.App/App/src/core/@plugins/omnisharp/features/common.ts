import {CancellationToken, editor, languages} from "monaco-editor";
import {IDisposable} from "@common";
import {IEventBus, MonacoEditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

/**
 * The script's editor model, if this window has created one.
 */
export function getScriptModel(scriptId: string): editor.ITextModel | null {
    return editor.getModel(MonacoEditorUtil.constructModelUri(scriptId));
}

/**
 * Calls back each time the OmniSharp server becomes ready for a script that an editor in this window shows.
 * Monaco can't ask a provider again for one model only, so firing a provider's change event re-asks for every
 * shown script. A script no editor shows is skipped and Monaco asks for it when an editor shows it.
 */
export function onServerReadyForShownScript(eventBus: IEventBus, callback: () => void): IDisposable {
    return eventBus.subscribeToServer(api.OmniSharpServerReadyEvent, ev => {
        if (getScriptModel(ev.scriptId)?.isAttachedToEditor()) {
            callback();
        }
    });
}

export async function findUsages(
    model: editor.ITextModel,
    omnisharpService: IOmniSharpService,
    lineNumber: number,
    column: number,
    excludeDefinition: boolean,
    token: CancellationToken): Promise<languages.Location[] | null> {

    const scriptId = MonacoEditorUtil.getScriptId(model);

    const response = await omnisharpService.findUsages(scriptId, new api.FindUsagesRequest({
        onlyThisFile: true,
        excludeDefinition: excludeDefinition,
        line: lineNumber,
        column: column,
        applyChangesTogether: false
    }), MonacoEditorUtil.abortSignalFrom(10000, token));

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
