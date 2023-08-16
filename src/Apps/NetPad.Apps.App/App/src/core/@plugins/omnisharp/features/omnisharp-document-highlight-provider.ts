import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {IDocumentHighlightProvider} from "@application";
import {FeatureProvider} from "./feature-provider";
import {findUsages} from "./common";

export class OmnisharpDocumentHighlightProvider extends FeatureProvider implements IDocumentHighlightProvider {
    public async provideDocumentHighlights(model: editor.ITextModel, position: Position, token: CancellationToken) {

        const references = await findUsages(
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
