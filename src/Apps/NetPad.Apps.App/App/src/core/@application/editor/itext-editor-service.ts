import {DI} from "aurelia";
import {ITextEditor} from "@application/editor/text-editor";

export interface ITextEditorService {
    get active(): ITextEditor | undefined;
    create(host: HTMLElement): ITextEditor;
    enableVimMode(): void;
    disableVimMode(): void;
}

export const ITextEditorService = DI.createInterface<ITextEditorService>();
