import {ITextEditor} from "./text-editor";
import {ITextEditorService} from "./itext-editor-service";

export class TextEditorService implements ITextEditorService {
    public active: ITextEditor | undefined;
}
