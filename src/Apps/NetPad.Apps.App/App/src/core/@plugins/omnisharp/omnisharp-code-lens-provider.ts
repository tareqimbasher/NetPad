import {languages, editor, Position, CancellationToken, Range} from "monaco-editor";
import {EditorUtil} from "@application";
import {CodeElement} from "@domain";
import {IOmniSharpService} from "./omnisharp-service";
import {OmnisharpReferenceProvider} from "./omnisharp-reference-provider";

export class OmnisharpCodeLensProvider implements languages.CodeLensProvider {
    private methodNamesToExclude = [
        "Equals",
        "Finalize",
        "GetHashCode",
        "ToString",
        "Dispose",
        "GetEnumerator",
    ];

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideCodeLenses(model: editor.ITextModel, token: CancellationToken): Promise<languages.CodeLensList> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getCodeStructure(scriptId);

        if (!response || !response.elements) {
            return {
                lenses: [],
                dispose: () => {
                }
            };
        }

        let results: languages.CodeLens[] = [];

        this.recurseCodeElements(response.elements, (element, parent) => {
            if (!this.shouldProvideCodeLens(element, parent)) {
                return;
            }

            const range = element.ranges["name"];

            if (range && range.start.line >= 0) {
                const codeLensItem: languages.CodeLens = {
                    range: new Range(range.start.line, range.start.column, range.end.line, range.end.column)
                };

                results.push(codeLensItem);
            }
        });

        return {
            lenses: results,
            dispose: () => {
            }
        };
    }

    public async resolveCodeLens(model: editor.ITextModel, codeLens: languages.CodeLens, token: CancellationToken): Promise<languages.CodeLens> {
        const references = await OmnisharpReferenceProvider.findUsages(
            model,
            this.omnisharpService,
            codeLens.range.startLineNumber,
            codeLens.range.startColumn);

        if (!references) {
            return null;
        }

        const count = references.length;

        codeLens.command = {
            title: count === 1 ? '1 reference' : `${count} references`,
            id: 'editor.action.showReferences',
            arguments: [model.uri, new Position(codeLens.range.startLineNumber, codeLens.range.startColumn), references]
        };

        return codeLens;
    }

    private shouldProvideCodeLens(element: CodeElement, parent?: CodeElement): boolean {
        if (element.kind === "namespace") {
            return false;
        }

        if (element.kind === "method" && this.methodNamesToExclude.indexOf(element.name) >= 0) {
            return false;
        }

        // We don't want to supply code lens for the Main method when script kind is Program
        if (element.kind === "method" && element.displayName == "Main()" && parent) {
            const parentRange = parent.ranges["name"];
            if (parentRange && parentRange.start.line < 1) {
                return false;
            }
        }

        return true;
    }

    private recurseCodeElements(elements: CodeElement[], action: (element: CodeElement, parentElement?: CodeElement) => void) {
        const walker = (elements: CodeElement[], parentElement?: CodeElement) => {
            for (let element of elements) {
                action(element, parentElement);

                if (element.children) {
                    walker(element.children, element);
                }
            }
        }

        walker(elements);
    }
}
