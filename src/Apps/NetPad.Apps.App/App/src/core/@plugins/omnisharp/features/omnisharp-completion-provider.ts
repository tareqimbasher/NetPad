import {CancellationToken, editor, IRange, languages} from "monaco-editor";
import {IScriptService, ISession} from "@domain";
import {EditorUtil, ICommandProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {TextChangeUtil} from "../utils";
import * as api from "../api";

export class OmniSharpCompletionProvider implements languages.CompletionItemProvider, ICommandProvider {
    public triggerCharacters = [".", " "];
    private lastCompletions?: Map<languages.CompletionItem, { model: editor.ITextModel, apiCompletionItem: api.CompletionItem }>;
    private readonly insertAdditionalTextEditsCommandId = "omnisharp.insertAdditionalTextEdits";

    constructor(
        @IOmniSharpService private readonly omnisharpService: IOmniSharpService,
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService) {
    }

    public provideCommands(): { id: string; handler: (accessor: unknown, ...args: unknown[]) => void; }[] {
        return [{
            id: this.insertAdditionalTextEditsCommandId,
            handler: (accessor: unknown, ...args: unknown[]) => {
                return this.insertAdditionalTextEdits(args[0] as editor.ITextModel, args[1] as api.LinePositionSpanTextChange[]);
            }
        }];
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

        const lastCompletions = new Map<languages.CompletionItem, { model: editor.ITextModel, apiCompletionItem: api.CompletionItem }>();

        for (let i = 0; i < results.monacoCompletions.length; i++) {
            lastCompletions.set(results.monacoCompletions[i], {
                model: model,
                apiCompletionItem: results.apiCompletions[i]
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

            const resolution = await this.omnisharpService.getCompletionResolution(scriptId, completion.apiCompletionItem);

            return this.convertToMonacoCompletionItem(completion.model, item.range as IRange, resolution.item);
        } catch (ex) {
            console.error(ex);
        }
    }

    private async getCompletionItems(model: editor.ITextModel, range: IRange, ctx: languages.CompletionContext, token: CancellationToken): Promise<CompletionResults> {
        const request = new api.CompletionRequest();
        request.line = range.startLineNumber;
        request.column = range.endColumn;
        request.completionTrigger = (ctx.triggerKind + 1) as unknown as api.CompletionTriggerKind;
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
            .map(omnisharpCompletion => this.convertToMonacoCompletionItem(model, range, omnisharpCompletion));

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        return {
            apiCompletions: omnisharpCompletions.items,
            monacoCompletions: monacoCompletions
        };
    }

    private convertToMonacoCompletionItem(model: editor.ITextModel, range: IRange, apiCompletion: api.CompletionItem): languages.CompletionItem {
        const kind = languages.CompletionItemKind[apiCompletion.kind];

        const newText = apiCompletion.textEdit?.newText ?? apiCompletion.label;

        const insertText = apiCompletion.insertTextFormat === "Snippet"
            ? newText // TODO might need to convert to a monaco compatible snippet
            : newText;

        let sortText = apiCompletion.sortText;
        if (kind === languages.CompletionItemKind.Property)
            sortText = "a" + sortText;
        else if (kind === languages.CompletionItemKind.Method)
            sortText = "b" + sortText;
        else
            sortText = "c" + sortText;

        // We don't want space to be a commit character for the suggestion, its annoying when one is typing
        // a variable name 'list' and hits space that the suggestion will be selected
        const commitCharacters = apiCompletion.commitCharacters?.filter(c => c != " ");

        const docs = apiCompletion.documentation ? {
            value: apiCompletion.documentation,
            isTrusted: false,
            supportHtml: true,
            supportThemeIcons: true
        } : undefined;

        const tags = apiCompletion.tags && apiCompletion.tags[0] === "Deprecated" ? 1 : [];

        let command: languages.Command = undefined;

        if (apiCompletion.hasAfterInsertStep) {
            command = {
                id: "csharp.completion.afterInsert",
                title: "",
                arguments: [apiCompletion]
            };
        }
        else if (apiCompletion.additionalTextEdits?.length > 0) {
            command = {
                id: this.insertAdditionalTextEditsCommandId,
                title: "Insert additional text",
                arguments: [model, apiCompletion.additionalTextEdits]
            };
        }

        return <languages.CompletionItem>{
            label: apiCompletion.label,
            detail: apiCompletion.detail,
            kind: kind,
            documentation: docs,
            commitCharacters: commitCharacters,
            preselect: apiCompletion.preselect,
            filterText: apiCompletion.filterText,
            insertText: insertText,
            range: range,
            tags: tags,
            sortText: sortText,
            additionalTextEdits: null,
            command: command
        };
    }

    private async insertAdditionalTextEdits(model: editor.ITextModel, additionalTextEdits: api.LinePositionSpanTextChange[]) {
        await TextChangeUtil.applyTextChanges(model, additionalTextEdits, this.session, this.scriptService);
    }
}

class CompletionResults {
    public apiCompletions: api.CompletionItem[] = [];
    public monacoCompletions: languages.CompletionItem[] = [];
}
