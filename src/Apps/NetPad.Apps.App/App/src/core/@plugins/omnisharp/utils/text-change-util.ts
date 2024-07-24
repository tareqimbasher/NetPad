import {editor} from "monaco-editor";
import {Util} from "@common";
import {IScriptService, ISession, MonacoEditorUtil} from "@application";
import {LinePositionSpanTextChange} from "../api";
import {Converter} from "./converter";

export class TextChangeUtil {
    public static async applyTextChanges(
        model: editor.ITextModel,
        textChanges: LinePositionSpanTextChange[],
        session: ISession,
        scriptService: IScriptService) {

        const editorLineCount = model.getLineCount();
        const scriptId = MonacoEditorUtil.getScriptId(model);
        const edits: editor.IIdentifiedSingleEditOperation[] = [];

        for (const textChange of textChanges) {
            const isOutOfEditorTextChange = (textChange.startLine < 1 && textChange.endLine < 1)
                || (textChange.startLine > editorLineCount)
                || this.isAddUsingChange(textChange);

            if (isOutOfEditorTextChange) {
                await this.processOutOfEditorRangeTextChange(scriptId, textChange, session, scriptService);
                continue;
            }

            if (textChange.startLine < 1) {
                // Discard text changes that occur before the first line
                textChange.newText = textChange.newText?.split("\n")
                    .slice(1 - textChange.startLine)
                    .join("\n");

                textChange.startLine = 1;
            }

            edits.push({
                text: textChange.newText || null,
                range: Converter.apiLinePositionSpanTextChangeToMonacoRange(textChange),
                forceMoveMarkers: false
            });
        }

        if (edits.length > 0) {
            // Using this instead of 'model.applyEdits()' so that undo stack is preserved
            model.pushEditOperations([], edits, () => []);
            model.pushStackElement();
        }
    }

    private static async processOutOfEditorRangeTextChange(
        scriptId: string,
        textChange: LinePositionSpanTextChange,
        session: ISession,
        scriptService: IScriptService) {

        const newNamespaces = this.getNamespacesFromUsings(textChange);

        if (newNamespaces.length) {
            const environment = await session.environments.find(e => e.script.id === scriptId);
            if (!environment) return;

            const namespaces = new Set<string>([...environment.script.config.namespaces]);
            for (const newNamespace of newNamespaces)
                namespaces.add(newNamespace);

            await scriptService.setScriptNamespaces(scriptId, [...namespaces]);
        }
    }

    private static isAddUsingChange(textChange: LinePositionSpanTextChange) {
        const newText = textChange.newText;

        if (!newText) return false;

        return (
            // For when we get the normal/expected text format, ex: "using System.Text.Json;\n\n"
            (newText.startsWith("using ") && textChange.startLine === 1)

            // For when OmniSharp server gives an unusual format, ex: "System.Text.Json;\n\nusing "
            || (newText.endsWith(";\n\nusing ") && textChange.startLine === 1 && textChange.endLine === 1)
        );
    }

    private static getNamespacesFromUsings(textChange: LinePositionSpanTextChange): string[] {
        return textChange.newText
            ?.split("\n")
            .map(l => l?.trim()) // to remove empty parts
            .filter(l => !!l)
            .map(l => Util.trimWord(l, "using "))
            .map(l => Util.trimEnd(l, ";"))
            .map(l => l.trim())
            .filter(l => l) || [];
    }
}
