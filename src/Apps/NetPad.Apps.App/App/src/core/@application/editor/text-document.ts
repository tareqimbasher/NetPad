import {SubscriptionToken, WithDisposables} from "@common";
import * as monaco from "monaco-editor";
import {EditorUtil} from "@application";
import {TextLanguage} from "@application/editor/text-editor";

export class TextDocument extends WithDisposables {
    public selection: monaco.Selection | null | undefined;

    private _text: string;
    private changeHandlers = new Set<(setter: unknown, newText: string) => Promise<void>>();
    private _textModel?: monaco.editor.ITextModel;

    constructor(public readonly id: string, public readonly language: TextLanguage, text: string) {
        super();
        if (!id) throw new Error(`${nameof(TextDocument)} id cannot be empty.`);
        this._text = text ?? ""
    }

    public get textModel(): monaco.editor.ITextModel {
        if (!this._textModel) {
            const language = this.language || "plaintext";
            const initialText = this._text;

            this._textModel = monaco.editor.createModel(
                initialText,
                language,
                EditorUtil.constructModelUri(this.id)
            );

            this.addDisposable(this._textModel.onDidChangeContent(async () => {
                if (this._textModel)
                    await this.setText(this, this._textModel.getValue());
            }));

            this.onChange(async (setter, newValue) => {
                if (this === setter || !this._textModel) return;
                this._textModel.setValue(newValue);
            });
        }

        return this._textModel;
    }

    public get text(): string {
        return this._text;
    }

    public async setText(setter: unknown, newValue: string) {
        if (this._text === newValue) return;

        this._text = newValue ?? "";

        for (const changeHandler of this.changeHandlers) {
            try {
                await changeHandler(setter, newValue);
            } catch (ex) {
                console.error(`Error while calling change handler on ${nameof(TextDocument)}`, changeHandler, ex);
            }
        }
    }

    public onChange(handler: (setter: unknown, newValue: string) => Promise<void>): SubscriptionToken {
        const wrappedHandler = (setter, newValue) => handler(setter, newValue);
        this.changeHandlers.add(wrappedHandler);
        return new SubscriptionToken(() => this.changeHandlers.delete(wrappedHandler));
    }

    public override toString() {
        return `${(this as Record<string, unknown>).constructor.name}: ${this.id}`;
    }

    public override dispose(): void {
        super.dispose();
        this.changeHandlers.clear();
        this._textModel?.dispose();
        this._textModel = undefined;
    }
}
