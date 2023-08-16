import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {IReferenceProvider} from "@application";
import {FeatureProvider} from "./feature-provider";
import {findUsages} from "./common";

export class OmniSharpReferenceProvider extends FeatureProvider implements IReferenceProvider {
    public async provideReferences(model: editor.ITextModel, position: Position, context: languages.ReferenceContext, token: CancellationToken)
        : Promise<languages.Location[]> {

        return await findUsages(model, this.omnisharpService, position.lineNumber, position.column, true, token) || [];
    }
}
