import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {QuickInfoRequest} from "../api";

export class OmnisharpHoverProvider implements languages.HoverProvider {
    constructor(@IOmniSharpService private omnisharpService: IOmniSharpService) {
    }

    public async provideHover(model: editor.ITextModel, position: Position, token: CancellationToken): Promise<languages.Hover> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getQuickInfo(scriptId, new QuickInfoRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }));

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
