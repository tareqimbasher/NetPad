import {CancellationToken, editor, IRange, languages} from "monaco-editor";
import {Util} from "@common";
import {IScriptService, ISession} from "@domain";
import {EditorUtil, ICommandProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {TextChangeUtil} from "../utils";
import {CompletionItem as OmnisharpCompletionItem, CompletionRequest, LinePositionSpanTextChange} from "../api";

export class OmnisharpCompletionProvider implements languages.CompletionItemProvider, ICommandProvider {
    public triggerCharacters = [".", " "];
    private lastCompletions?: Map<languages.CompletionItem, { model: editor.ITextModel, omnisharpCompletionItem: OmnisharpCompletionItem }>;
    private readonly insertAdditionalTextEditsCommandId = "omnisharp.insertAdditionalTextEdits";

    constructor(
        @IOmniSharpService private readonly omnisharpService: IOmniSharpService,
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService) {
    }

    public provideCommands(): { id: string; handler: (accessor: any, ...args: any[]) => void; }[] {
        return [{
            id: this.insertAdditionalTextEditsCommandId,
            handler: (accessor: any, ...args: any[]) => {
                return this.insertAdditionalTextEdits(args[0], args[1]);
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

        const lastCompletions = new Map<languages.CompletionItem, { model: editor.ITextModel, omnisharpCompletionItem: OmnisharpCompletionItem }>();

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

            return this.convertToMonacoCompletionItem(completion.model, item.range as IRange, resolution.item);
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
            .map(omnisharpCompletion => this.convertToMonacoCompletionItem(model, range, omnisharpCompletion));

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        return {
            omniSharpCompletions: omnisharpCompletions.items,
            monacoCompletions: monacoCompletions
        };
    }

    private convertToMonacoCompletionItem(model: editor.ITextModel, range: IRange, omnisharpCompletion: OmnisharpCompletionItem): languages.CompletionItem {
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

        const tags = omnisharpCompletion.tags && omnisharpCompletion.tags[0] === "Deprecated" ? 1 : [];

        let command: languages.Command = undefined;

        if (omnisharpCompletion.hasAfterInsertStep) {
            command = {
                id: "csharp.completion.afterInsert",
                title: "",
                arguments: [omnisharpCompletion]
            };
        }
        else if (omnisharpCompletion.additionalTextEdits?.length > 0) {
            command = {
                id: this.insertAdditionalTextEditsCommandId,
                title: "Insert additional text",
                arguments: [model, omnisharpCompletion.additionalTextEdits]
            };
        }

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
            additionalTextEdits: null,
            command: command
        };
    }

    private async insertAdditionalTextEdits(model: editor.ITextModel, additionalTextEdits: LinePositionSpanTextChange[]) {
        await TextChangeUtil.applyTextChanges(model, additionalTextEdits, this.session, this.scriptService);
    }
}

class CompletionResults {
    public omniSharpCompletions: OmnisharpCompletionItem[] = [];
    public monacoCompletions: languages.CompletionItem[] = [];
}
