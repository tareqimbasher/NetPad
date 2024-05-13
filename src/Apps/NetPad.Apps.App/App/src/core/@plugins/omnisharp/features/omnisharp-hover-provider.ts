import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {MonacoEditorUtil, IHoverProvider} from "@application";
import * as api from "../api";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpHoverProvider extends FeatureProvider implements IHoverProvider {
    public async provideHover(model: editor.ITextModel, position: Position, token: CancellationToken): Promise<languages.Hover> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getQuickInfo(scriptId, new api.QuickInfoRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }), this.getAbortSignal(token));

        if (!response || !response.markdown) {
            return {contents: []};
        }

        return {
            contents: [{
                value: response.markdown,
                isTrusted: true,
                supportThemeIcons: true,
                supportHtml: true
            }],
        }
    }
}
