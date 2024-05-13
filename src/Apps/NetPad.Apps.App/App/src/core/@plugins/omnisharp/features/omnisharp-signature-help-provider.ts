import {CancellationToken, editor, IMarkdownString, languages, Position} from "monaco-editor";
import {MonacoEditorUtil, ISignatureHelpProvider} from "@application";
import * as api from "../api";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpSignatureHelpProvider extends FeatureProvider implements ISignatureHelpProvider {
    public signatureHelpTriggerCharacters = ["(", ","];

    public async provideSignatureHelp(model: editor.ITextModel, position: Position, token: CancellationToken, context: languages.SignatureHelpContext)
        : Promise<languages.SignatureHelpResult> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getSignatureHelp(scriptId, new api.SignatureHelpRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }), this.getAbortSignal(token));

        if (!response || !response.signatures) {
            // Interface does not allow returning of undefined, but it is allowed.
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            return undefined as any;
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
                label: signature.label || "",
                documentation: signature.structuredDocumentation?.summaryText,
                parameters: []
            }

            result.value.signatures.push(signatureInfo);

            if (signature.parameters) {
                for (const parameter of signature.parameters) {
                    const parameterInfo: languages.ParameterInformation = {
                        label: parameter.label || "",
                        documentation: this.getParameterDocumentation(parameter)
                    };

                    signatureInfo.parameters.push(parameterInfo);
                }
            }
        }

        return result;
    }

    private getParameterDocumentation(parameter: api.SignatureHelpParameter): string | IMarkdownString {
        const summary = parameter.documentation;
        if (summary && summary.length > 0) {
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
