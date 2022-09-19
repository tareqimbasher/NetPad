import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil, IHoverProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";

export class OmniSharpHoverProvider implements IHoverProvider {
    constructor(@IOmniSharpService private omnisharpService: IOmniSharpService) {
    }

    public async provideHover(model: editor.ITextModel, position: Position, token: CancellationToken): Promise<languages.Hover> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getQuickInfo(scriptId, new api.QuickInfoRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }), new AbortController().signalFrom(token));

        if (!response || !response.markdown) {
            return null;
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
