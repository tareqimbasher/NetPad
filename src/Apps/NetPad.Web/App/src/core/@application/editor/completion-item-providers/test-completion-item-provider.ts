import * as monaco from "monaco-editor";

export class TestCompletionItemProvider {
    public register() {
        monaco.languages.registerCompletionItemProvider("csharp", {
            provideCompletionItems: (model, position, ctx, token) => {
                return <any>{
                    suggestions: [
                        {
                            label: "Console.WriteLine",
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            documentation: "Write a line to the console",
                            inertText: "Console.WriteLine();",
                            range: {
                                replace: {
                                    startLineNumber: position.lineNumber,
                                    endLineNumber: position.lineNumber,
                                    startColumn: position.column,
                                    endColumn: position.column
                                }
                            }
                        }
                    ],
                    incomplete: false
                };
            }
        })
    }
}
