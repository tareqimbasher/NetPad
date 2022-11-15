import {
    IEventBus,
    ScriptEnvironment,
    ScriptSqlOutputEmittedEvent,
    ScriptStatus
} from "@domain";
import {ViewModelBase} from "@application";
import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";

export class SqlView extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @bindable public active: boolean;

    private outputElement: HTMLElement;
    private textWrap: boolean;

    constructor(@IEventBus private readonly eventBus: IEventBus, @ILogger logger: ILogger) {
        super(logger);
    }

    public attached() {
        const token = this.eventBus.subscribeToServer(ScriptSqlOutputEmittedEvent, msg => {
            if (msg.scriptId === this.environment.script.id) {
                this.appendResults(msg.output);
            }
        });
        this.disposables.push(() => token.dispose());
    }

    private appendResults(results: string | null | undefined) {
        if (!results) return;

        let output = results
            .replaceAll(" ", "&nbsp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\n", "<br/>");

        output += "<br/><br/>";

        const template = document.createElement("template");
        template.innerHTML = output;
        this.outputElement.appendChild(template.content);
    }

    private clearResults() {
        while (this.outputElement.firstChild && this.outputElement.lastChild)
            this.outputElement.removeChild(this.outputElement.lastChild);
    }

    @watch<SqlView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearResults();
    }
}
