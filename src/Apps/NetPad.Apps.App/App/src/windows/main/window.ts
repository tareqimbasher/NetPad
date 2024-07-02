import {IContainer} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    CreateScriptDto,
    DataConnectionStore,
    IPaneManager,
    IScriptService,
    ISession,
    IShortcutManager,
    MonacoEnvironmentManager,
    PaneHost,
    PaneHostOrientation
} from "@application";
import {ClipboardPane, CodePane, Explorer, NamespacesPane, OutputPane, SplitViewController} from "./panes";
import {Workbench} from "./workbench";
import {WindowBase} from "@application/windows/window-base";

export class Window extends WindowBase {
    private workbench: Workbench
    public leftPaneHost: PaneHost;
    public rightPaneHost: PaneHost;
    public bottomPaneHost: PaneHost;

    constructor(
        @ISession private readonly session: ISession,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IPaneManager private readonly paneManager: IPaneManager,
        @IScriptService private readonly scriptService: IScriptService,
        @IContainer private readonly container: IContainer,
        private readonly dataConnectionStore: DataConnectionStore) {
        super();
    }

    public hydrating() {
        this.shortcutManager.initialize();
    }

    public async binding() {
        await MonacoEnvironmentManager.setupMonacoEnvironment(this.container);
        await this.session.initialize();
        await this.dataConnectionStore.initialize();
        this.workbench = this.container.get(Workbench);

        await this.createNewScriptIfNoScriptsOpen();
    }

    public attached() {
        const middleContentElement = document.getElementById("window-middle-content");
        const workAreaElement = middleContentElement?.querySelector("work-area") as HTMLElement;

        if (!middleContentElement || !workAreaElement) throw new Error("Could not find required elements");

        const sideToSideController = new SplitViewController(
            () => [this.leftPaneHost, middleContentElement, this.rightPaneHost],
            "horizontal",
            15
        );

        this.leftPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Left, sideToSideController);
        this.rightPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Right, sideToSideController);

        const explorer = this.paneManager.addPaneToHost(Explorer, this.leftPaneHost);
        this.paneManager.addPaneToHost(NamespacesPane, this.rightPaneHost);
        this.paneManager.addPaneToHost(ClipboardPane, this.rightPaneHost);


        const topBottomController = new SplitViewController(
            () => [workAreaElement, this.bottomPaneHost],
            "vertical",
            50
        );

        this.bottomPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Bottom, topBottomController);
        const outputPane = this.paneManager.addPaneToHost(OutputPane, this.bottomPaneHost);
        this.paneManager.addPaneToHost(CodePane, this.bottomPaneHost);


        // Start explorer expanded by default
        if (!sideToSideController.hasSavedState()) {
            setTimeout(() => explorer.activate(), 1);
        }

        // Always start output pane hidden when app starts
        setTimeout(() => outputPane.hide(), 1);
    }

    @watch<Window>(vm => vm.session.environments.length)
    private async createNewScriptIfNoScriptsOpen() {
        if (this.session.environments.length === 0) {
            await this.scriptService.create(new CreateScriptDto());
        }
    }
}
