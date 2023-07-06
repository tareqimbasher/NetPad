import {CancellationToken, editor, Emitter, IEvent, languages, Position} from "monaco-editor";
import {EditorUtil, ICodeLensProvider} from "@application";
import {OmniSharpReferenceProvider} from "./omnisharp-reference-provider";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

export class OmniSharpCodeLensProvider implements ICodeLensProvider {
    private methodNamesToExclude = [
        "Equals",
        "Finalize",
        "GetHashCode",
        "ToString",
        "Dispose",
        "GetEnumerator",
    ];
    private _onDidChange: Emitter<this>;

    public onDidChange: IEvent<this>;

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
        this._onDidChange = new Emitter<this>();
        this.onDidChange = this._onDidChange.event;
    }

    public async provideCodeLenses(model: editor.ITextModel, token: CancellationToken): Promise<languages.CodeLensList> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getCodeStructure(scriptId, new AbortController().signalFrom(token));

        if (!response || !response.elements) {
            return {
                lenses: [],
                dispose: () => {
                    // do nothing
                }
            };
        }

        const results: languages.CodeLens[] = [];

        this.recurseCodeElements(response.elements, (element, parent) => {
            if (!this.shouldProvideCodeLens(element, parent)) {
                return;
            }

            const range = element.ranges["name"];

            if (range && range.start && range.start.line >= 0) {
                const codeLensItem: languages.CodeLens = {
                    range: Converter.apiRangeToMonacoRange(range)
                };

                results.push(codeLensItem);
            }
        });

        return {
            lenses: results,
            dispose: () => {
                // do nothing
            }
        };
    }

    public async resolveCodeLens(model: editor.ITextModel, codeLens: languages.CodeLens, token: CancellationToken): Promise<languages.CodeLens> {
        const references = await OmniSharpReferenceProvider.findUsages(
            model,
            this.omnisharpService,
            codeLens.range.startLineNumber,
            codeLens.range.startColumn,
            true,
            token);

        if (!references) {
            return codeLens;
        }

        const count = references.length;

        codeLens.command = {
            title: count === 1 ? '1 reference' : `${count} references`,
            id: 'editor.action.showReferences',
            arguments: [model.uri, new Position(codeLens.range.startLineNumber, codeLens.range.startColumn), references]
        };

        return codeLens;
    }

    private shouldProvideCodeLens(element: api.CodeElement, parent?: api.CodeElement): boolean {
        if (element.kind === "namespace") {
            return false;
        }

        if (element.kind === "method" && this.methodNamesToExclude.indexOf(element.name) >= 0) {
            return false;
        }

        return true;
    }

    private recurseCodeElements(elements: api.CodeElement[], action: (element: api.CodeElement, parentElement?: api.CodeElement) => void) {
        const walker = (elements: api.CodeElement[], parentElement?: api.CodeElement) => {
            for (const element of elements) {
                action(element, parentElement);

                if (element.children) {
                    walker(element.children, element);
                }
            }
        }

        walker(elements);
    }
}
