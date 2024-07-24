import {customElement, watch} from "aurelia";
import {ISession} from "@application";
import {FloatingPaneWindowBase, windowTemplate} from "@application/windows/floating-pane-window-base";
import {CodePane} from "../main/panes";

@customElement({
    name: 'floating-window',
    template: windowTemplate
})
export class Window extends FloatingPaneWindowBase<CodePane> {
    constructor(@ISession private readonly session: ISession) {
        super(CodePane, "Code");
    }

    public async binding() {
        await this.session.initialize();
        this.updateTitle();
    }

    @watch<Window>(vm => vm.session.active)
    private updateTitle() {
        document.title = !this.session.active
            ? this.windowName
            : `${this.session.active.script.name} - ${this.windowName}`;
    }
}
