import * as monaco from "monaco-editor";
import {CancellationToken, editor, languages, Position, Range} from "monaco-editor";
import {ICompletionItemProvider} from "./interfaces";
import {snippets} from "./snippets/csharp";
import {TextLanguage} from "../text-language";

export class BuiltinCSharpCompletionProvider implements ICompletionItemProvider {
    // This must remain as "undefined", otherwise it will interfere with suggestions provided by omnisharp
    public triggerCharacters: string[] | undefined = undefined;
    private completionItems: languages.CompletionItem[] = [];

    constructor() {
        this.init();
    }

    public get language(): TextLanguage {
        return "csharp";
    }

    public provideCompletionItems(model: editor.ITextModel, position: Position, context: languages.CompletionContext, token: CancellationToken): languages.ProviderResult<languages.CompletionList> {
        const word = model.getWordUntilPosition(position);
        const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
        };

        // Copy completion items and set range
        const completionItems = JSON.parse(JSON.stringify(this.completionItems)) as languages.CompletionItem[];
        for (const completionItem of completionItems) {
            completionItem.range = range;
        }

        return {
            suggestions: completionItems
        };
    }

    private init(): void {
        const completionItems: languages.CompletionItem[] = [];
        const defaultRange = new Range(0, 0, 0, 0);

        for (const key in snippets) {
            const snippet = snippets[key as keyof typeof snippets];

            completionItems.push({
                label: snippet.prefix,
                documentation: {
                    value: `<i>Snippet:</i> <b>${snippet.description}</b>`,
                    isTrusted: false,
                    supportHtml: true,
                    supportThemeIcons: true
                },
                insertText: snippet.body.join("\n"),
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                sortText: snippet.prefix,
                range: defaultRange
            });
        }

        this.completionItems = completionItems;
    }
}
