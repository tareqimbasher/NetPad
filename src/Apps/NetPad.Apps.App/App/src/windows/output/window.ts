import {customElement, watch} from "aurelia";
import {ISession} from "@application";
import {FloatingPaneWindowBase, windowTemplate} from "@application/windows/floating-pane-window-base";
import {OutputPane} from "../main/panes";
import {IOutputModelDto} from "../main/panes/output-pane/output-model";

@customElement({
    name: 'floating-window',
    template: windowTemplate
})
export class Window extends FloatingPaneWindowBase<OutputPane> {
    constructor(@ISession private readonly session: ISession) {
        super(OutputPane, "Output");
    }

    public async binding() {
        await this.session.initialize();
        this.updateTitle();

        // Get outputs from output pane in main window
        const bc = new BroadcastChannel("output-window");
        bc.onmessage = (ev) => {
            const dtos = ev.data as IOutputModelDto[];
            if (!dtos || !Array.isArray(dtos) || dtos.length === 0) {
                return;
            }

            for (const dto of dtos) {
                const scriptId = dto.scriptId;

                const model = this.pane.outputModels.get(scriptId);

                if (!model) {
                    continue;
                }

                model.inputRequest = dto.inputRequest;

                model.resultsDumpContainer.setHtml(dto.resultsDumpContainer.html);
                model.resultsDumpContainer.lastOutputOrder = dto.resultsDumpContainer.lastOutputOrder;
                model.resultsDumpContainer.scrollOnOutput = dto.resultsDumpContainer.scrollOnOutput;
                model.resultsDumpContainer.textWrap = dto.resultsDumpContainer.textWrap;

                model.sqlDumpContainer.setHtml(dto.sqlDumpContainer.html);
                model.sqlDumpContainer.lastOutputOrder = dto.sqlDumpContainer.lastOutputOrder;
                model.sqlDumpContainer.scrollOnOutput = dto.sqlDumpContainer.scrollOnOutput;
                model.sqlDumpContainer.textWrap = dto.sqlDumpContainer.textWrap;
            }

            // We only need this message once
            bc.close();
        };

        bc.postMessage("send-outputs");
    }

    @watch<Window>(vm => vm.session.active)
    private updateTitle() {
        document.title = !this.session.active
            ? this.windowName
            : `${this.session.active.script.name} - ${this.windowName}`;
    }
}
