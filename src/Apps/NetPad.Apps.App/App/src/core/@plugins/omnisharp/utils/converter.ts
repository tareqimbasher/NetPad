import * as api from "../api";
import * as mco from "monaco-editor";

export class Converter {
    public static monacoIPositionToApiPoint(position: mco.IPosition): api.Point {
        return new api.Point({
            line: position.lineNumber,
            column: position.column
        });
    }

    public static apiPointToMonacoIPosition(point: api.IPoint): mco.IPosition {
        return {
            lineNumber: point.line,
            column: point.column
        };
    }

    public static apiRangeToMonacoRange(range: api.Range): mco.Range {
        if (!range.start || !range.end)
            throw new Error("range start or end is undefined or null");

        return new mco.Range(
            range.start.line,
            range.start.column,
            range.end.line,
            range.end.column);
    }

    public static monacoRangeToApiRange(range: mco.Range): api.Range {
        return new api.Range({
            start: new api.Point({line: range.startLineNumber, column: range.startColumn}),
            end: new api.Point({line: range.endLineNumber, column: range.endColumn})
        });
    }

    public static apiLinePositionSpanTextChangeToMonacoRange(textChange: api.LinePositionSpanTextChange): mco.Range {
        return new mco.Range(
            textChange.startLine,
            textChange.startColumn,
            textChange.endLine,
            textChange.endColumn
        );
    }

    public static apiLinePositionSpanTextChangeToMonacoTextEdit(textChange: api.LinePositionSpanTextChange): mco.languages.TextEdit {
        return {
            text: textChange.newText ?? "",
            range: Converter.apiLinePositionSpanTextChangeToMonacoRange(textChange)
        };
    }

    public static apiSemanticHighlightSpanToMonacoRange(span: api.SemanticHighlightSpan): mco.Range {
        return new mco.Range(
            span.startLine,
            span.startColumn,
            span.endLine,
            span.endColumn);
    }

    public static apiQuickFixToMonacoRange(quickFix: api.QuickFix): mco.Range {
        return new mco.Range(quickFix.line, quickFix.column, quickFix.endLine, quickFix.endColumn);
    }
}
