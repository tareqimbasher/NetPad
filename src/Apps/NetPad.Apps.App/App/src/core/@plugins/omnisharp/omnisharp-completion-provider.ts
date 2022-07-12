import {CompletionItem as OmnisharpCompletionItem, CompletionRequest} from "./api";
import {CancellationToken, editor, IRange, languages} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "./omnisharp-service";

export class OmnisharpCompletionProvider implements languages.CompletionItemProvider {
    public triggerCharacters = [".", " "];
    private lastCompletions?: Map<languages.CompletionItem, {model: editor.ITextModel, omnisharpCompletionItem: OmnisharpCompletionItem}>;

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideCompletionItems(model, position, ctx, token) {
        const word = model.getWordUntilPosition(position);
        const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
        };

        const results = await this.getCompletionItems(model, range, ctx, token);

        const lastCompletions = new Map<languages.CompletionItem, {model: editor.ITextModel, omnisharpCompletionItem: OmnisharpCompletionItem}>();

        for (let i = 0; i < results.monacoCompletions.length; i++) {
            lastCompletions.set(results.monacoCompletions[i], {
                model: model,
                omnisharpCompletionItem: results.omniSharpCompletions[i]
            });
        }

        this.lastCompletions = lastCompletions;

        return {
            suggestions: results.monacoCompletions
        };
    }

    public async resolveCompletionItem(item: languages.CompletionItem, token: CancellationToken) {
        try {
            if (!this.lastCompletions) {
                return item;
            }

            const completion = this.lastCompletions.get(item);
            if (!completion) {
                return item;
            }

            const scriptId = EditorUtil.getScriptId(completion.model);

            const resolution = await this.omnisharpService.getCompletionResolution(scriptId, completion.omnisharpCompletionItem);

            return this.convertToMonacoCompletionItem(item.range as IRange, resolution.item);
        } catch (ex) {
            console.error(ex);
        }
    }

    private async getCompletionItems(model: editor.ITextModel, range: IRange, ctx: languages.CompletionContext, token: CancellationToken): Promise<CompletionResults> {
        const request = new CompletionRequest();
        request.line = range.startLineNumber - 1;
        request.column = range.endColumn - 1;
        request.completionTrigger = (ctx.triggerKind + 1) as any;
        request.triggerCharacter = ctx.triggerCharacter;

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        const scriptId = EditorUtil.getScriptId(model);

        const omnisharpCompletions = await this.omnisharpService.getCompletion(scriptId, request);

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
        const kind = languages.CompletionItemKind[omnisharpCompletion.kind];

        const newText = omnisharpCompletion.textEdit?.newText ?? omnisharpCompletion.label;

        const insertText = omnisharpCompletion.insertTextFormat === "Snippet"
            ? newText // TODO might need to convert to a monaco compatible snippet
            : newText;

        let sortText = omnisharpCompletion.sortText;
        if (kind === languages.CompletionItemKind.Property)
            sortText = "a" + sortText;
        else if (kind === languages.CompletionItemKind.Method)
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
            return <editor.ISingleEditOperation>{
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

class CompletionResults {
    public omniSharpCompletions: OmnisharpCompletionItem[] = [];
    public monacoCompletions: languages.CompletionItem[] = [];
}
