import {
    resolveScriptStatusIndicator,
    ScriptEnvironment,
    ScriptStatusIndicator,
    scriptStatusIndicatorTitle,
    ScriptSummary,
    Settings
} from "@application";

export class ScriptViewModel extends ScriptSummary {
    constructor(summary: ScriptSummary, private readonly settings: Settings) {
        super(summary);
    }

    public environment?: ScriptEnvironment;
    public isActive: boolean;

    public get isDirty(): boolean {
        return this.environment?.script.isDirty === true;
    }

    public get statusIndicator(): ScriptStatusIndicator | undefined {
        return resolveScriptStatusIndicator(this.environment);
    }

    public get statusTitle(): string {
        return scriptStatusIndicatorTitle(this.statusIndicator);
    }

    public get showStatusDot(): boolean {
        const indicator = this.statusIndicator;
        if (!indicator) return false;

        return indicator === "running" || indicator === "stopping"
            ? this.settings.appearance.showScriptRunningIndicatorInScriptsList
            : this.settings.appearance.showScriptRunStatusIndicatorInScriptsList;
    }

    public get cssClasses(): string {
        let classes = this.isActive ? "is-active selected" : "";

        if (this.environment) {
            classes += " is-open";
            if (this.environment.script.isDirty) classes += " is-dirty";
            if (this.environment.status === "Ready") classes += " is-ready";
            else if (this.environment.status === "Running") classes += " is-running";
            else if (this.environment.status === "Error") classes += " is-error";
        }

        return classes;
    }
}
