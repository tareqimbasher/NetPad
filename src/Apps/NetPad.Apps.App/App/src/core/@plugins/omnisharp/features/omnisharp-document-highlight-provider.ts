import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {IDocumentHighlightProvider} from "@application";
import {OmniSharpReferenceProvider} from "./omnisharp-reference-provider";
import {IOmniSharpService} from "../omnisharp-service";

export class OmnisharpDocumentHighlightProvider implements IDocumentHighlightProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideDocumentHighlights(model: editor.ITextModel, position: Position, token: CancellationToken) {

        const references = await OmniSharpReferenceProvider.findUsages(
            model,
            this.omnisharpService,
            position.lineNumber,
            position.column,
            false,
            token);

        const highlights: languages.DocumentHighlight[] = [];

        if (!references) {
            return highlights;
        }

        for (const reference of references) {
            highlights.push({
                range: reference.range,
                kind: languages.DocumentHighlightKind.Read
            });
        }

        return highlights;
    }
}
