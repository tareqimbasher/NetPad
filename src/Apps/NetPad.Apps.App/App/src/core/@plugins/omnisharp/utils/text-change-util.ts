import {editor} from "monaco-editor";
import {Util} from "@common";
import {IScriptService, ISession} from "@domain";
import {EditorUtil} from "@application";
import {LinePositionSpanTextChange} from "../api";
import {Converter} from "./converter";

export class TextChangeUtil {
    public static async applyTextChanges(
        model: editor.ITextModel,
        textChanges: LinePositionSpanTextChange[],
        session: ISession,
        scriptService: IScriptService) {

        const editorLineCount = model.getLineCount();
        const scriptId = EditorUtil.getScriptId(model);
        const edits: editor.IIdentifiedSingleEditOperation[] = [];

        for (const textChange of textChanges) {
            const isOutOfEditorTextChange = (textChange.startLine < 1 && textChange.endLine < 1)
                || (textChange.startLine > editorLineCount)
                || textChange.newText.startsWith("using ");

            if (isOutOfEditorTextChange) {
                await this.processOutOfEditorRangeTextChange(scriptId, textChange, session, scriptService);
                continue;
            }

            if (textChange.startLine < 1) {
                // Discard text changes that occur before the first line
                textChange.newText = textChange.newText.split("\n")
                    .slice(1 - textChange.startLine)
                    .join("\n");

                textChange.startLine = 1;
            }

            edits.push({
                text: textChange.newText,
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

        const newLines = textChange.newText.split("\n")
            .map(l => l.trim())
            .filter(l => l);

        if (newLines.filter(l => l.startsWith("using ")).length == newLines.length) {
            const environment = await session.environments.find(e => e.script.id === scriptId);
            if (environment) {
                const namespaces = new Set<string>([...environment.script.config.namespaces]);

                for (const newLine of newLines) {
                    let namespace = newLine.slice("using ".length - 1);
                    namespace = Util.trimEnd(namespace, ";").trim();
                    namespaces.add(namespace);
                }

                await scriptService.setScriptNamespaces(scriptId, [...namespaces]);
            }
        }
    }
}
