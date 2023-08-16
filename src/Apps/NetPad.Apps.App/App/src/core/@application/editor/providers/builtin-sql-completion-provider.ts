import {ICompletionItemProvider, TextLanguage} from "@application";
import {CancellationToken, editor, languages, Position} from "monaco-editor";

export class BuiltinSqlCompletionProvider implements ICompletionItemProvider {
    public triggerCharacters = [" "];
    private static keywords = [
        "SHOW",
        "DROP",
        "SELECT",
        "UPDATE",
        "CREATE",
        "DELETE",
        "INSERT",
        "REPLACE",
        "EXPLAIN",
        "ALL",
        "DISTINCT",
        "AS",
        "TABLE",
        "INTO",
        "FROM",
        "SET",
        "LEFT",
        "ON",
        "INNER",
        "JOIN",
        "UNION",
        "VALUES",
        "EXISTS",
        "WHERE",
        "GROUP",
        "BY",
        "HAVING",
        "ORDER",
        "ASC",
        "DESC",
        "TOP",
        "LIMIT",
        "BETWEEN",
        "IN",
        "IS",
        "LIKE",
        "CONTAINS",
        "NOT",
        "AND",
        "OR",
    ];

    private static completionItems: languages.CompletionItem[];

    public get language(): TextLanguage {
        return "sql";
    }

    public provideCompletionItems(model: editor.ITextModel, position: Position, ctx: languages.CompletionContext, token: CancellationToken) {
        const word = model.getWordUntilPosition(position);
        const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
        };

        if (!BuiltinSqlCompletionProvider.completionItems) {
            BuiltinSqlCompletionProvider.completionItems = BuiltinSqlCompletionProvider.keywords.map(x => {
                return {
                    label: x,
                    insertText: x,
                    range: range,
                    kind: languages.CompletionItemKind.Keyword,
                    sortText: x
                }
            });
        } else {
            BuiltinSqlCompletionProvider.completionItems.forEach(item => item.range = range);
        }

        return {
            suggestions: BuiltinSqlCompletionProvider.completionItems
        };
    }
}
