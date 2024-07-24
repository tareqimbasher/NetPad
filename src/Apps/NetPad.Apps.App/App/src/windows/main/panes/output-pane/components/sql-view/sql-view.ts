import {watch} from "@aurelia/runtime-html";
import {ScriptStatus} from "@application";
import {OutputViewBase} from "../output-view-base";

export class SqlView extends OutputViewBase {
    @watch<SqlView>(vm => vm.model.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running") {
            this.model.sqlDumpContainer.clearOutput(true);
        }
    }
}

