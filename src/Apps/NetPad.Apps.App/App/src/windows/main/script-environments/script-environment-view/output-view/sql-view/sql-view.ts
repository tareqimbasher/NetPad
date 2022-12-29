import {HtmlScriptOutput, IEventBus, ScriptSqlOutputEmittedEvent, ScriptStatus} from "@domain";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {OutputViewBase} from "../output-view-base";

export class SqlView extends OutputViewBase {
    private textWrap: boolean;

    constructor(@IEventBus private readonly eventBus: IEventBus, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        const token = this.eventBus.subscribeToServer(ScriptSqlOutputEmittedEvent, msg => {
            if (msg.scriptId === this.environment.script.id) {
                if (!msg.output) return;

                const output = JSON.parse(msg.output) as HtmlScriptOutput;
                this.appendOutput(output);
            }
        });
        this.disposables.push(() => token.dispose());
    }

    @watch<SqlView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearOutput();
    }
}
