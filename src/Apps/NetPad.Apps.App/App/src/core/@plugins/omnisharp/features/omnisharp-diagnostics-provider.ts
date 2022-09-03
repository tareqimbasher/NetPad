import {CancellationTokenSource, editor, MarkerSeverity, MarkerTag} from "monaco-editor";
import {IEventBus, Settings} from "@domain";
import {EditorUtil, IDiagnosticsProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";

export class OmnisharpDiagnosticsProvider implements IDiagnosticsProvider {
    private readonly unnecessaryMarkerTag = MarkerTag[MarkerTag.Unnecessary].toLowerCase();
    private readonly excluded = new Set<string>([
        "IDE0008" // Use explicit type instead of "var"
    ]);

    constructor(
        @IOmniSharpService private omnisharpService: IOmniSharpService,
        @IEventBus private readonly eventBus: IEventBus,
        private readonly settings: Settings) {
    }

    public async provideDiagnostics(model: editor.ITextModel, setMarkers: (diagnostics: editor.IMarkerData[]) => void) {
        console.debug(`TIPS provideDiagnostics called`);

        const scriptId = EditorUtil.getScriptId(model);

        let cancellationTokenSource = new CancellationTokenSource();

        this.eventBus.subscribeToServer(api.OmniSharpDiagnosticsEvent, ev => {
            console.debug(`TIPS DiagnosticsEvent received`);

            cancellationTokenSource.dispose(true);
            cancellationTokenSource = new CancellationTokenSource();
            const token = cancellationTokenSource.token;

            if (ev.scriptId !== scriptId) {
                return;
            }

            const markers: editor.IMarkerData[] = [];

            for (const quickFix of ev.diagnostics.results.flatMap(r => r.quickFixes)) {
                if (token.isCancellationRequested) {
                    return;
                }

                if (this.excluded.has(quickFix.id)) {
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

        await this.omnisharpService.startDiagnostics(scriptId);
    }

    private getDisplay(quickFix: api.DiagnosticLocation, severity: MarkerSeverity | "hidden") {
        // Even when analyzers are disabled, we want to fadeout these values
        const isFadeout = (quickFix.tags && !!quickFix.tags.find(x => x.toLowerCase() == this.unnecessaryMarkerTag))
            || quickFix.id == "CS0162"  // CS0162: Unreachable code
            || quickFix.id == "CS0219"  // CS0219: Unused variable
            || quickFix.id == "CS8019"; // CS8019: Unnecessary using

        if (isFadeout && quickFix.logLevel.toLowerCase() === "hidden" || quickFix.logLevel.toLowerCase() === "none") {
            // Roslyn uses hidden, Monaco does not
            return {severity: MarkerSeverity.Hint, isFadeout};
        }

        return {severity: severity, isFadeout};
    }

    private getDiagnosticSeverity(quickFix: api.DiagnosticLocation): MarkerSeverity | "hidden" {
        switch (quickFix.logLevel.toLowerCase()) {
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
