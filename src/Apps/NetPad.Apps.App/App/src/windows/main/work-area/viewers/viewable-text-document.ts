import {IViewableObjectCommands, ViewableObject, ViewableObjectType} from "./viewable-object";
import {TextLanguage} from "@application/editor/text-language";
import {TextDocument} from "@application/editor/text-document";

export class ViewableTextDocument extends ViewableObject {
    private readonly _name: string;
    private readonly _language: TextLanguage;
    private readonly _initialText: string;
    protected _textDocument?: TextDocument;

    constructor(
        id: string,
        name: string,
        language: TextLanguage,
        initialText: string,
        commands: IViewableObjectCommands,
    ) {
        super(
            id,
            ViewableObjectType.Text,
            commands
        );
        this._name = name;
        this._language = language;
        this._initialText = initialText;
    }

    public get name() {
        return this._name;
    }

    public get isDirty() {
        return this._textDocument != null && this._initialText != this._textDocument.text;
    }

    public get textDocument(): TextDocument {
        if (!this._textDocument) {
            this._textDocument = new TextDocument(this.id, this._language, this._initialText);
            this.addDisposable(() => this._textDocument?.dispose());
        }

        return this._textDocument;
    }
}
