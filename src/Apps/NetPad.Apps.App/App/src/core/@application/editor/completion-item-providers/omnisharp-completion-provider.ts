import {CompletionItem as OmnisharpCompletionItem, CompletionRequest, IOmniSharpService, ISession} from "@domain";
import * as monaco from "monaco-editor";
import {CancellationToken, editor, IRange, languages} from "monaco-editor";
import ISingleEditOperation = editor.ISingleEditOperation;
import CompletionItemKind = languages.CompletionItemKind;
import MonacoCompletionItemKind = languages.CompletionItemKind;

export class OmnisharpCompletionProvider {
    private lastCompletions?: Map<languages.CompletionItem, OmnisharpCompletionItem>;

    constructor(
        @ISession private readonly session: ISession,
        @IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public register() {
        monaco.languages.registerCompletionItemProvider("csharp", {
            triggerCharacters: ["."],
            provideCompletionItems: async (model, position, ctx, token) => {
                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                const results = await this.getCompletionItems(range, ctx, token);

                const lastCompletions = new Map<languages.CompletionItem, OmnisharpCompletionItem>();

                for (let i = 0; i < results.monacoCompletions.length; i++) {
                    lastCompletions.set(results.monacoCompletions[i], results.omniSharpCompletions[i]);
                }

                this.lastCompletions = lastCompletions;

                return {
                    suggestions: results.monacoCompletions
                };
            },
            resolveCompletionItem: async (item: languages.CompletionItem, token: CancellationToken) => {
                try {
                    if (!this.lastCompletions) {
                        return item;
                    }

                    const omnisharpCompletionItem = this.lastCompletions.get(item);
                    if (!omnisharpCompletionItem) {
                        return item;
                    }

                    const resolution = await this.omnisharpService.getCompletionResolution(this.session.active.script.id, omnisharpCompletionItem);

                    return this.convertToMonacoCompletionItem(item.range as IRange, resolution.item);
                }
                catch (ex) {
                    console.error(ex);
                }
            }
        });
    }

    private async getCompletionItems(range: IRange, ctx: languages.CompletionContext, token: CancellationToken) : Promise<CompletionResults> {
        const request = new CompletionRequest();
        request.line = range.startLineNumber - 1;
        request.column = range.endColumn - 1;
        request.completionTrigger = (ctx.triggerKind + 1) as any;
        request.triggerCharacter = ctx.triggerCharacter;

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        const omnisharpCompletions = await this.omnisharpService.getCompletion(this.session.active.script.id, request);

        if (token.isCancellationRequested || !omnisharpCompletions || !omnisharpCompletions.items) {
            return new CompletionResults();
        }

        const monacoCompletions = omnisharpCompletions.items
            .map(omnisharpCompletion => this.convertToMonacoCompletionItem(range, omnisharpCompletion));

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        return {
            omniSharpCompletions: omnisharpCompletions.items,
            monacoCompletions: monacoCompletions
        };
    }

    private convertToMonacoCompletionItem(range: IRange, omnisharpCompletion: OmnisharpCompletionItem): languages.CompletionItem {
        const kind = MonacoCompletionItemKind[omnisharpCompletion.kind];

        const newText = omnisharpCompletion.textEdit?.newText ?? omnisharpCompletion.label;

        const insertText = omnisharpCompletion.insertTextFormat === "Snippet"
            ? newText // TODO might need to convert to a monaco compatible snippet
            : newText;

        let sortText = omnisharpCompletion.sortText;
        if (kind === CompletionItemKind.Property)
            sortText = "a" + sortText;
        else if (kind === CompletionItemKind.Method)
            sortText = "b" + sortText;
        else
            sortText = "c" + sortText;

        // We don't want space to be a commit character for the suggestion, its annoying when one is typing
        // a variable name 'list' and hits space that the suggestion will be selected
        const commitCharacters = omnisharpCompletion.commitCharacters?.filter(c => c != " ");

        const docs = omnisharpCompletion.documentation ? {
            value: omnisharpCompletion.documentation,
            isTrusted: false,
            supportHtml: true,
            supportThemeIcons: true
        } : undefined;

        const additionalTextEdits = omnisharpCompletion.additionalTextEdits?.map(ate => {
            return <ISingleEditOperation>{
                text: ate.newText,
                range: {
                    startLineNumber: ate.startLine,
                    startColumn: ate.startColumn,
                    endLineNumber: ate.endLine,
                    endColumn: ate.endColumn
                }
            };
        });

        const tags = omnisharpCompletion.tags && omnisharpCompletion.tags[0] === "Deprecated" ? 1 : [];

        return <languages.CompletionItem>{
            label: omnisharpCompletion.label,
            detail: omnisharpCompletion.detail,
            kind: kind,
            documentation: docs,
            commitCharacters: commitCharacters,
            preselect: omnisharpCompletion.preselect,
            filterText: omnisharpCompletion.filterText,
            insertText: insertText,
            range: range,
            tags: tags,
            sortText: sortText,
            additionalTextEdits: additionalTextEdits,
            command: omnisharpCompletion.hasAfterInsertStep ? {
                id: "",
                command: "csharp.completion.afterInsert",
                title: "",
                arguments: [omnisharpCompletion]
            } : undefined
        };
    }
}

class CompletionResults
{
    public omniSharpCompletions: OmnisharpCompletionItem[] = [];
    public monacoCompletions: languages.CompletionItem[] = [];
}
