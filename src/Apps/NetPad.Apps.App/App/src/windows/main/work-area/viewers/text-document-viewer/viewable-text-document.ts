import {IViewableObjectCommands, ViewableObject, ViewableObjectType} from "../viewable-object";
import {ScriptEnvironment} from "@domain";
import {TextLanguage} from "@application/editor/text-editor";
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

export interface IViewableAppScriptDocumentCommands extends IViewableObjectCommands {
    updateCode: (newCode: string) => Promise<void>
    run: () => Promise<void>;
    stop: () => Promise<void>;
    openProperties: () => Promise<void>;
}

export class ViewableAppScriptDocument extends ViewableTextDocument {
    constructor(
        public readonly environment: ScriptEnvironment,
        protected override readonly commands: IViewableAppScriptDocumentCommands,
    ) {
        super(
            environment.script.id,
            environment.script.name,
            "csharp",
            environment.script.code,
            commands,
        );
    }

    public override get name() {
        return this.environment.script.name;
    }

    public override get isDirty() {
        return this.environment.script.isDirty;
    }

    public get script() {
        return this.environment.script;
    }

    public override get textDocument(): TextDocument {
        const alreadyCreated = this._textDocument !== null && this._textDocument !== undefined;
        const textDocument = super.textDocument;

        if (!alreadyCreated) {
            this.addDisposable(
                textDocument.onChange(async () => await this.textChanged()));
        }

        return textDocument;
    }

    private async textChanged() {
        const code = this.textDocument.text;
        this.script.code = code;

        await this.commands.updateCode(code);
    }

    public run(): Promise<void> {
        return this.commands.run();
    }

    public stop(): Promise<void> {
        return this.commands.stop();
    }

    public openProperties(): Promise<void> {
        return this.commands.openProperties();
    }
}
