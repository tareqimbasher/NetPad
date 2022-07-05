import {DI} from "aurelia";
import {languages} from "monaco-editor";

export const ICompletionItemProvider = DI.createInterface<languages.CompletionItemProvider>();
export const IDocumentSemanticTokensProvider = DI.createInterface<languages.DocumentSemanticTokensProvider>();
export const IDocumentRangeSemanticTokensProvider = DI.createInterface<languages.DocumentRangeSemanticTokensProvider>();
export const IImplementationProvider = DI.createInterface<languages.ImplementationProvider>();
export const IHoverProvider = DI.createInterface<languages.HoverProvider>();
