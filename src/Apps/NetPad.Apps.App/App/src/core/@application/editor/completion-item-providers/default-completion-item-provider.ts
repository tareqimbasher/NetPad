import * as monaco from "monaco-editor";
import {IRange, languages} from "monaco-editor";
import CompletionItem = languages.CompletionItem;

export class DefaultCompletionItemProvider {
    private createCompletionItems(range: IRange): CompletionItem[] {
        return [
            {
                label: "Console.WriteLine",
                kind: monaco.languages.CompletionItemKind.Function,
                documentation: "Write a line to the console",
                insertText: "Console.WriteLine(${1});",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "cw",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Write a line to the console",
                insertText: "Console.WriteLine(${1});",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "if",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "If statement",
                insertText: "if (${1})\n{\n\t\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "for",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "For loop",
                insertText: "for (int ${1:i} = 0; i < ${2:10}; ${1}++)\n{\n\t${3}\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "foreach",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Foreach loop",
                insertText: "foreach (var ${2:item} in ${1:collection})\n{\n\t${3}\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "try",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Try/Catch",
                insertText: "try\n{\n\t\n}\ncatch (Exception ${1:e})\n{\n\t\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
        ];
    }

    public register() {
        monaco.languages.registerCompletionItemProvider("csharp", {
            provideCompletionItems: (model, position, ctx, token) => {
                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                return {
                    suggestions: this.createCompletionItems(range)
                };
            }
        })
    }
}
