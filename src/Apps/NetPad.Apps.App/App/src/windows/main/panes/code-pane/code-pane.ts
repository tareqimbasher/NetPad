import {watch} from "@aurelia/runtime-html";
import {IWindowService, Pane, PaneIds} from "@application";
import {AppWindows} from "@application/windowing/app-windows";

export class CodePane extends Pane {
    constructor(@IWindowService private readonly windowService: IWindowService, private readonly appWindows: AppWindows) {
        super(PaneIds.code, "Code", "code");
    }

    private async openExternalCodeWindow() {
        this.hide();
        await this.windowService.openCodeWindow();
    }

    @watch<CodePane>(vm => vm.appWindows.items.map(x => x.name))
    private reactToExternalWindowState(currentWindowNames: string, previousWindowNames: string) {
        if (this.isWindow) {
            return;
        }

        const wasOpen = previousWindowNames.indexOf("code") >= 0;
        const currentlyOpen = currentWindowNames.indexOf("code") >= 0;
        const hostHasAnActivePaneOpen = this.host?.active;

        if (wasOpen && !currentlyOpen && !hostHasAnActivePaneOpen) {
            this.activate();
        } else if (currentlyOpen) {
            this.hide();
        }
    }
}
