import {ScriptEnvironment, ScriptSummary} from "@application";

export class ScriptViewModel extends ScriptSummary {
    constructor(summary: ScriptSummary) {
        super(summary);
    }

    public environment?: ScriptEnvironment;
    public isActive: boolean;

    public get cssClasses(): string {
        let classes = this.isActive ? 'is-active' : "";

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
