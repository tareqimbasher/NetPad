import {CancellationToken, editor, languages} from "monaco-editor";
import {MonacoEditorUtil, IDocumentSymbolProvider} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {Symbols} from "../types";
import {FeatureProvider} from "./feature-provider";

export class OmnisharpDocumentSymbolProvider extends FeatureProvider implements IDocumentSymbolProvider {
    public async provideDocumentSymbols(model: editor.ITextModel, token: CancellationToken): Promise<languages.DocumentSymbol[]> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getCodeStructure(scriptId, this.getAbortSignal(token));

        if (!response || !response.elements) {
            return [];
        }

        return this.createSymbols(response.elements);
    }

    private createSymbols(elements: api.CodeElement[], parentElement?: api.CodeElement): languages.DocumentSymbol[] {
        const results: languages.DocumentSymbol[] = [];

        elements.forEach(element => {
            const symbol = this.createSymbol(element, parentElement);
            if (element.children) {
                symbol.children = this.createSymbols(element.children, element);
            }

            results.push(symbol);
        });

        return results;
    }

    private createSymbol(element: api.CodeElement, parentElement?: api.CodeElement): languages.DocumentSymbol {
        const fullRange = element.ranges[Symbols.RangeNames.Full];
        const nameRange = element.ranges[Symbols.RangeNames.Name];
        const name = this.getNameForElement(element, parentElement);
        const details = name === element.displayName ? '' : element.displayName;

        return {
            name: name,
            detail: details,
            kind: Converter.apiSymbolKindToMonacoSymbolKind(element.kind),
            range: Converter.apiRangeToMonacoRange(fullRange),
            selectionRange: Converter.apiRangeToMonacoRange(nameRange),
            tags: [],
        };
    }

    private getNameForElement(element: api.CodeElement, parentElement?: api.CodeElement) {
        switch (element.kind) {
            case Symbols.OmniSharpKinds.Class:
            case Symbols.OmniSharpKinds.Delegate:
            case Symbols.OmniSharpKinds.Enum:
            case Symbols.OmniSharpKinds.Interface:
            case Symbols.OmniSharpKinds.Struct:
                return element.name;

            case Symbols.OmniSharpKinds.Namespace:
                return typeof parentElement !== 'undefined' && element.displayName.startsWith(`${parentElement.displayName}.`)
                    ? element.displayName.slice(parentElement.displayName.length + 1)
                    : element.displayName;

            case Symbols.OmniSharpKinds.Constant:
            case Symbols.OmniSharpKinds.Constructor:
            case Symbols.OmniSharpKinds.Destructor:
            case Symbols.OmniSharpKinds.EnumMember:
            case Symbols.OmniSharpKinds.Event:
            case Symbols.OmniSharpKinds.Field:
            case Symbols.OmniSharpKinds.Indexer:
            case Symbols.OmniSharpKinds.Method:
            case Symbols.OmniSharpKinds.Operator:
            case Symbols.OmniSharpKinds.Property:
            case Symbols.OmniSharpKinds.Unknown:
            default:
                return element.displayName;
        }
    }
}





