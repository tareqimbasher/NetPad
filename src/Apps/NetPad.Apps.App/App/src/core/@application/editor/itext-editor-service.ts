import {DI} from "aurelia";
import {ITextEditor} from "@application/editor/text-editor";

export interface ITextEditorService {
    active: ITextEditor | undefined;
}

export const ITextEditorService = DI.createInterface<ITextEditorService>();
