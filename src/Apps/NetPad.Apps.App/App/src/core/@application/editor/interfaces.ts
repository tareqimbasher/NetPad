import {DI} from "aurelia";
import {languages, editor} from "monaco-editor";

export interface ICommandProvider {
    provideCommands(): {id: string, handler: (accessor: unknown, ...args: unknown[]) => void}[];
}

export const ICommandProvider = DI.createInterface<ICommandProvider>();

export interface IDiagnosticsProvider {
    provideDiagnostics(model: editor.ITextModel, setMarkers: (diagnostics: editor.IMarkerData[]) => void);
}

export const IDiagnosticsProvider = DI.createInterface<IDiagnosticsProvider>();


export const ICompletionItemProvider = DI.createInterface<languages.CompletionItemProvider>();
export const IDocumentSemanticTokensProvider = DI.createInterface<languages.DocumentSemanticTokensProvider>();
export const IDocumentRangeSemanticTokensProvider = DI.createInterface<languages.DocumentRangeSemanticTokensProvider>();
export const IImplementationProvider = DI.createInterface<languages.ImplementationProvider>();
export const IHoverProvider = DI.createInterface<languages.HoverProvider>();
export const ISignatureHelpProvider = DI.createInterface<languages.SignatureHelpProvider>();
export const IReferenceProvider = DI.createInterface<languages.ReferenceProvider>();
export const ICodeLensProvider = DI.createInterface<languages.CodeLensProvider>();
export const IInlayHintsProvider = DI.createInterface<languages.InlayHintsProvider>();
export const ICodeActionProvider = DI.createInterface<languages.CodeActionProvider>();
