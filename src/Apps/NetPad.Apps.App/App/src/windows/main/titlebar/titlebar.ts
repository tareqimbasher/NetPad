import {
    IAppService,
    ISession,
    ISettingsService,
    IShortcutManager,
    IWindowService,
    Settings,
    ViewModelBase,
    WindowState
} from "@application";
import {Util} from "@common";
import {ILogger} from "aurelia";
import {AppUpdateDialog} from "@application/app/app-update-dialog/app-update-dialog";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class Titlebar extends ViewModelBase {
    public windowState: WindowState;
    private readonly updateWindowState: () => void;

    constructor(@ISession private readonly session: ISession,
                @IWindowService private readonly windowService: IWindowService,
                @IAppService private readonly appService: IAppService,
                @ISettingsService private readonly settingsService: ISettingsService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                private readonly settings: Settings,
                private readonly dialogUtil: DialogUtil,
                @ILogger logger: ILogger
    ) {
        super(logger);

        this.updateWindowState = Util.debounce(this, async () => {
            this.windowState = await this.windowService.getState();
        }, 500, true);
    }

    private get leftWinCtrls(): boolean {
        return this.settings.appearance.titlebar.windowControlsPosition === "Left";
    }

    public get title() {
        const activeScriptName = this.session.active?.script.name;

        return !activeScriptName ? "NetPad" : activeScriptName + " - NetPad";
    }

    public async bound() {
        this.updateWindowState();

        const handler = () => this.updateWindowState();
        window.addEventListener("resize", handler);
        this.addDisposable(() => window.removeEventListener("resize", handler));
        // TODO when a window is un-maximized (restored), the handler might not execute on the 'resize' event, depending on OS

        document.addEventListener("visibilitychange", handler);
        this.addDisposable(() => document.removeEventListener("visibilitychange", handler));
    }

    public async minimize() {
        await this.windowService.minimize();
        this.updateWindowState();
    }

    public async maximize() {
        await this.windowService.maximize();
        this.updateWindowState();
    }

    public close() {
        window.close();
    }

    public async toggleWindowAlwaysOnTop() {
        await this.windowService.toggleAlwaysOnTop();
        this.updateWindowState();
    }

    public async openSettingsWindow() {
        await this.settingsService.openSettingsWindow(null);
    }

    public async openAppUpdateDialog() {
        await this.dialogUtil.toggle(AppUpdateDialog);
    }
}
