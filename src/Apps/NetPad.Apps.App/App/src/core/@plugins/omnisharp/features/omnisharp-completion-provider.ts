import {CancellationToken, editor, IRange, languages, Position} from "monaco-editor";
import {
    ICommandProvider,
    ICompletionItemProvider,
    IScriptService,
    ISession,
    MonacoEditorUtil,
    TextLanguage
} from "@application";
import {Converter, TextChangeUtil} from "../utils";
import * as api from "../api";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpCompletionProvider extends FeatureProvider implements ICompletionItemProvider, ICommandProvider {
    public triggerCharacters = [".", " "];
    private lastCompletions?: Map<languages.CompletionItem, {
        model: editor.ITextModel,
        apiCompletionItem: api.CompletionItem
    }>;
    private readonly insertAdditionalTextEditsCommandId = "netpad.command.omnisharp.insertAdditionalTextEdits";

    constructor(
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService) {
        super();
    }

    public get language(): TextLanguage {
        return "csharp";
    }

    public provideCommands(): { id: string; handler: (accessor: unknown, ...args: unknown[]) => void; }[] {
        return [{
            id: this.insertAdditionalTextEditsCommandId,
            handler: (accessor: unknown, ...args: unknown[]) => {
                return this.insertAdditionalTextEdits(args[0] as editor.ITextModel, args[1] as api.LinePositionSpanTextChange[]);
            }
        }];
    }

    public async provideCompletionItems(model: editor.ITextModel, position: Position, ctx: languages.CompletionContext, token: CancellationToken) {
        const word = model.getWordUntilPosition(position);
        const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
        };

        const results = await this.getCompletionItems(model, range, ctx, token);

        const lastCompletions = new Map<languages.CompletionItem, {
            model: editor.ITextModel,
            apiCompletionItem: api.CompletionItem
        }>();

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

            const scriptId = MonacoEditorUtil.getScriptId(completion.model);

            const resolution = await this.omnisharpService.getCompletionResolution(scriptId, completion.apiCompletionItem, this.getAbortSignal(token));

            if (!resolution || !resolution.item) {
                return item;
            }

            return this.convertToMonacoCompletionItem(completion.model, item.range as IRange, resolution.item);
        } catch (ex) {
            console.error("Error resolving CompletionItem", item, ex);
            return item;
        }
    }

    private async getCompletionItems(model: editor.ITextModel, range: IRange, ctx: languages.CompletionContext, token: CancellationToken): Promise<CompletionResults> {
        const request = new api.CompletionRequest();
        request.line = range.startLineNumber;
        request.column = range.endColumn;
        request.completionTrigger =
            ctx.triggerKind === languages.CompletionTriggerKind.Invoke
                ? "Invoked"
                : ctx.triggerKind === languages.CompletionTriggerKind.TriggerCharacter
                    ? "TriggerCharacter"
                    : "TriggerForIncompleteCompletions";
        request.triggerCharacter = ctx.triggerCharacter;

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        const scriptId = MonacoEditorUtil.getScriptId(model);

        const omnisharpCompletions = await this.omnisharpService.getCompletion(scriptId, request, this.getAbortSignal(token));

        if (token.isCancellationRequested || !omnisharpCompletions || !omnisharpCompletions.items) {
            return new CompletionResults();
        }

        const apiCompletions = omnisharpCompletions.items
            .filter(c => !c.detail || c.detail?.startsWith("NetPad.Media") || !c.detail.startsWith("NetPad."));

        const monacoCompletions = apiCompletions
            .map(omnisharpCompletion => this.convertToMonacoCompletionItem(model, range, omnisharpCompletion));

        if (token.isCancellationRequested) {
            return new CompletionResults();
        }

        return {
            apiCompletions: apiCompletions,
            monacoCompletions: monacoCompletions
        };
    }

    private convertToMonacoCompletionItem(model: editor.ITextModel, range: IRange, apiCompletion: api.CompletionItem): languages.CompletionItem {
        const kind = languages.CompletionItemKind[apiCompletion.kind];

        const insertRange = apiCompletion.textEdit
            ? Converter.apiLinePositionSpanTextChangeToMonacoRange(apiCompletion.textEdit)
            : range;

        const insertTextRules = apiCompletion.insertTextFormat == "Snippet"
            ? languages.CompletionItemInsertTextRule.InsertAsSnippet
            : languages.CompletionItemInsertTextRule.KeepWhitespace;

        const insertText = apiCompletion.textEdit?.newText ?? apiCompletion.label;

        let sortText = apiCompletion.sortText;

        if (kind === languages.CompletionItemKind.Keyword && sortText?.startsWith("0"))
            // Move the '0' to the end so that keyword suggestion falls after snippet with same name (if any)
            sortText = sortText.substring(1) + "0";

        // We don't want space to be a commit character for the suggestion, its annoying when one is typing
        // a variable name ex. 'list' and hits space, that the suggestion will be selected
        const commitCharacters = apiCompletion.commitCharacters?.filter(c => c != " ");

        const docs = apiCompletion.documentation ? {
            value: apiCompletion.documentation,
            isTrusted: false,
            supportHtml: true,
            supportThemeIcons: true
        } : undefined;

        const tags = apiCompletion.tags && apiCompletion.tags[0] === "Deprecated" ? [1] : [];

        let command: languages.Command | undefined = undefined;

        if (apiCompletion.hasAfterInsertStep) {
            command = {
                id: "csharp.completion.afterInsert",
                title: "",
                arguments: [apiCompletion]
            };
        } else if (apiCompletion.additionalTextEdits && apiCompletion.additionalTextEdits.length > 0) {
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
            insertTextRules: insertTextRules,
            range: insertRange,
            tags: tags,
            sortText: sortText,
            additionalTextEdits: undefined,
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
