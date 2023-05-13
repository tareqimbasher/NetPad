import {DI} from "aurelia";
import {editor, languages} from "monaco-editor";
import IActionDescriptor = editor.IActionDescriptor;

export interface ICompletionItemProvider extends languages.CompletionItemProvider {
}

export const ICompletionItemProvider = DI.createInterface<ICompletionItemProvider>();

export interface IDocumentSemanticTokensProvider extends languages.DocumentSemanticTokensProvider {
}

export const IDocumentSemanticTokensProvider = DI.createInterface<IDocumentSemanticTokensProvider>();

export interface IDocumentRangeSemanticTokensProvider extends languages.DocumentRangeSemanticTokensProvider {
}

export const IDocumentRangeSemanticTokensProvider = DI.createInterface<IDocumentRangeSemanticTokensProvider>();

export interface IImplementationProvider extends languages.ImplementationProvider {
}

export const IImplementationProvider = DI.createInterface<IImplementationProvider>();

export interface IHoverProvider extends languages.HoverProvider {
}

export const IHoverProvider = DI.createInterface<IHoverProvider>();

export interface ISignatureHelpProvider extends languages.SignatureHelpProvider {
}

export const ISignatureHelpProvider = DI.createInterface<ISignatureHelpProvider>();

export interface IReferenceProvider extends languages.ReferenceProvider {
}

export const IReferenceProvider = DI.createInterface<IReferenceProvider>();

export interface ICodeLensProvider extends languages.CodeLensProvider {
}

export const ICodeLensProvider = DI.createInterface<ICodeLensProvider>();

export interface IInlayHintsProvider extends languages.InlayHintsProvider {
}

export const IInlayHintsProvider = DI.createInterface<IInlayHintsProvider>();

export interface ICodeActionProvider extends languages.CodeActionProvider {
}

export const ICodeActionProvider = DI.createInterface<ICodeActionProvider>();

export interface IDiagnosticsProvider {
    provideDiagnostics(model: editor.ITextModel, setMarkers: (diagnostics: editor.IMarkerData[]) => void);
}

export const IDiagnosticsProvider = DI.createInterface<IDiagnosticsProvider>();

export interface ICommandProvider {
    provideCommands(): {
        id: string,
        handler: (accessor: { get: (service: unknown) => unknown }, ...args: unknown[]) => void
    }[];
}

export const ICommandProvider = DI.createInterface<ICommandProvider>();

export interface IActionProvider {
    provideActions(): IActionDescriptor[];
}

export const IActionProvider = DI.createInterface<IActionProvider>();
