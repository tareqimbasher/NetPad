import * as monaco from "monaco-editor";
import {IRange, languages} from "monaco-editor";
import CompletionItem = languages.CompletionItem;
import {ICompletionItemProvider} from "@application";

export class BuiltinCompletionProvider implements ICompletionItemProvider {
    public triggerCharacters = undefined;

    public provideCompletionItems(model, position, ctx, token) {
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

    private createCompletionItems(range: IRange): CompletionItem[] {
        return [
            {
                label: "cw",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Write a line to the console",
                insertText: "Console.WriteLine(${TM_SELECTED_TEXT}$0);",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "if",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "If statement",
                insertText: "if (${1:true})\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "else",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Else statement",
                insertText: "else\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "for",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "For loop",
                insertText: "for (int ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "forr",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Reverse for loop",
                insertText: "for (int ${1:i} = ${2:length} - 1; ${1:i} >= 0 ; ${1:i}--)\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "foreach",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Foreach loop",
                insertText: "foreach (${1:var} ${2:item} in ${3:collection})\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "while",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "While loop",
                insertText: "while (${1:true})\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "do",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "do...while loop",
                insertText: "do\n{\n\t${TM_SELECTED_TEXT}$0\n} while (${1:true});",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "switch",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Switch statement",
                insertText: "switch (${1:switch_on})\n{\n\t$0\n\tdefault:\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "try",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Try catch",
                insertText: "try\n{\n\t${TM_SELECTED_TEXT}$1\n}\ncatch (${2:System.Exception})\n{\n\t$0\n\tthrow;\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "tryf",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Try finally",
                insertText: "try\n{\n\t${TM_SELECTED_TEXT}$1\n}\nfinally\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "using",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Using statement",
                insertText: "using (${1:resource})\n{\n\t${TM_SELECTED_TEXT}$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "class",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Class",
                insertText: "class ${1:Name}\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "struct",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Struct",
                insertText: "struct ${1:Name}\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "interface",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Interface",
                insertText: "interface I${1:Name}\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "enum",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "Enum",
                insertText: "enum ${1:Name}\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
            {
                label: "ctor",
                kind: monaco.languages.CompletionItemKind.Snippet,
                documentation: "constructor",
                insertText: "${1:public} ${2:ClassName}(${3:Parameters})\n{\n\t$0\n}",
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range: range
            },
        ];
    }
}
