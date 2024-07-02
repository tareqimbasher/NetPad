import {CancellationTokenSource, editor, MarkerSeverity, MarkerTag} from "monaco-editor";
import {IDiagnosticsProvider, IEventBus, MonacoEditorUtil, Settings} from "@application";
import * as api from "../api";
import {FeatureProvider} from "./feature-provider";

export class OmnisharpDiagnosticsProvider extends FeatureProvider implements IDiagnosticsProvider {
    private readonly unnecessaryMarkerTag = MarkerTag[MarkerTag.Unnecessary].toLowerCase();
    private readonly excluded = new Set<string>([
        "IDE0008",          // Use explicit type instead of "var",
        "CA1050",           // Declare types in namespaces
    ]);

    constructor(
        @IEventBus private readonly eventBus: IEventBus,
        private readonly settings: Settings) {
        super();
    }

    public async provideDiagnostics(model: editor.ITextModel, setMarkers: (diagnostics: editor.IMarkerData[]) => void) {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        let cancellationTokenSource = new CancellationTokenSource();

        this.eventBus.subscribeToServer(api.OmniSharpDiagnosticsEvent, ev => {

            cancellationTokenSource.dispose(true);
            cancellationTokenSource = new CancellationTokenSource();
            const token = cancellationTokenSource.token;

            if (ev.scriptId !== scriptId || !ev.diagnostics.results) {
                return;
            }

            const markers: editor.IMarkerData[] = [];

            for (const quickFix of ev.diagnostics.results.flatMap(r => r.quickFixes)) {
                if (!quickFix || token.isCancellationRequested) {
                    return;
                }

                if (quickFix.id && this.excluded.has(quickFix.id)) {
                    continue;
                }

                const display = this.getDisplay(quickFix, this.getDiagnosticSeverity(quickFix));

                if (display.severity === "hidden") {
                    continue;
                }

                const marker: editor.IMarkerData = {
                    code: quickFix.id,
                    source: "csharp",
                    message: `[${MarkerSeverity[display.severity]}] ` + quickFix.text,
                    severity: display.severity,
                    startLineNumber: quickFix.line,
                    startColumn: quickFix.column,
                    endLineNumber: quickFix.endLine,
                    endColumn: quickFix.endColumn
                };

                if (display.isFadeout) {
                    marker.tags = [MarkerTag.Unnecessary]
                }

                markers.push(marker);
            }

            if (token.isCancellationRequested) {
                return;
            }

            setMarkers(markers);
        });

        await this.omnisharpService.startDiagnostics(scriptId, new AbortController().signalFromDefaultTimeout());
    }

    private getDisplay(quickFix: api.DiagnosticLocation, severity: MarkerSeverity | "hidden") {
        // Even when analyzers are disabled, we want to fadeout these values
        const isFadeout = (quickFix.tags && !!quickFix.tags.find(x => x.toLowerCase() == this.unnecessaryMarkerTag))
            || quickFix.id == "CS0162"  // CS0162: Unreachable code
            || quickFix.id == "CS0219"  // CS0219: Unused variable
            || quickFix.id == "CS8019"; // CS8019: Unnecessary using

        if (isFadeout && quickFix.logLevel?.toLowerCase() === "hidden" || quickFix.logLevel?.toLowerCase() === "none") {
            // Roslyn uses hidden, Monaco does not
            return {severity: MarkerSeverity.Hint, isFadeout};
        }

        return {severity: severity, isFadeout};
    }

    private getDiagnosticSeverity(quickFix: api.DiagnosticLocation): MarkerSeverity | "hidden" {
        switch (quickFix.logLevel?.toLowerCase()) {
            case "error":
                return MarkerSeverity.Error;
            case "warning":
                return this.settings.omniSharp.diagnostics.enableWarnings ? MarkerSeverity.Warning : "hidden";
            case "info":
                return this.settings.omniSharp.diagnostics.enableInfo ? MarkerSeverity.Info : "hidden";
            case "hidden":
                return this.settings.omniSharp.diagnostics.enableHints ? MarkerSeverity.Hint : "hidden";
            default:
                return "hidden";
        }
    }
}
