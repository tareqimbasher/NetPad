import {CancellationToken, editor, IMarkdownString, languages, Position} from "monaco-editor";
import {EditorUtil, ISignatureHelpProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";

export class OmniSharpSignatureHelpProvider implements ISignatureHelpProvider {
    public signatureHelpTriggerCharacters = ["(", ","];

    constructor(@IOmniSharpService private omnisharpService: IOmniSharpService) {
    }

    public async provideSignatureHelp(model: editor.ITextModel, position: Position, token: CancellationToken, context: languages.SignatureHelpContext)
        : Promise<languages.SignatureHelpResult> {
        const scriptId = EditorUtil.getScriptId(model);


        const response = await this.omnisharpService.getSignatureHelp(scriptId, new api.SignatureHelpRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }), new AbortController().signalFrom(token));

        if (!response || !response.signatures) {
            return null;
        }

        const result: languages.SignatureHelpResult = {
            value: {
                activeSignature: response.activeSignature,
                activeParameter: response.activeParameter,
                signatures: []
            },
            dispose: () => {
                // do nothing
            }
        };

        for (const signature of response.signatures) {
            const signatureInfo: languages.SignatureInformation = {
                label: signature.label,
                documentation: signature.structuredDocumentation.summaryText,
                parameters: []
            }

            result.value.signatures.push(signatureInfo);

            for (const parameter of signature.parameters) {
                const parameterInfo: languages.ParameterInformation = {
                    label: parameter.label,
                    documentation: this.getParameterDocumentation(parameter)
                };

                signatureInfo.parameters.push(parameterInfo);
            }
        }

        return result;
    }

    private getParameterDocumentation(parameter: api.SignatureHelpParameter): string | IMarkdownString {
        const summary = parameter.documentation;
        if (summary.length > 0) {
            const paramText = `**${parameter.name}**: ${summary}`;
            return <IMarkdownString>{
                value: paramText,
                isTrusted: true,
                supportThemeIcons: true,
                supportHtml: true
            };
        }

        return "";
    }
}
