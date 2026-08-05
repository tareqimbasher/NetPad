import {bindable, ILogger} from "aurelia";
import {IAppService, IPackageService, Settings, ThemeBackground, ThemeMode, ViewModelBase} from "@application";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";
import {AppTheme, ThemeFamily, ThemeGround} from "@application/themes/app-theme";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {Util} from "@common";

interface ModeChoice {
    mode: ThemeMode;
    label: string;
}

export class GeneralSettings extends ViewModelBase {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    public appVersion?: string;
    public cacheSize?: string;
    public cachePackageCount?: number;
    public updateStatus?: string;

    constructor(
        currentSettings: Settings,
        @IAppService private readonly appService: IAppService,
        @IPackageService private readonly packageService: IPackageService,
        @INativeDialogService private readonly nativeDialogService: INativeDialogService,
        private readonly dialogUtil: DialogUtil,
        @ILogger logger: ILogger) {
        super(logger);
        this.currentSettings = currentSettings;
    }

    public attached() {
        void this.loadCacheInfo();
        void this.loadUpdateStatus();
    }

    public readonly families: readonly ThemeFamily[] = AppTheme.families;

    public readonly modeChoices: ModeChoice[] = [
        {mode: "Dark", label: "dark"},
        {mode: "Light", label: "light"},
        {mode: "System", label: "system"},
    ];

    public themeCssClass(family: string, ground: ThemeGround): string {
        return AppTheme.cssClass(family, ground);
    }

    public themeCssClasses(family: string, background: ThemeBackground, ground: ThemeGround): string {
        return AppTheme.cssClasses({family, ground, background});
    }

    public get titlebarTypeNeedsRestart(): boolean {
        return this.settings.appearance.titlebar.type !== this.currentSettings.appearance.titlebar.type;
    }

    public get mainMenuNeedsRestart(): boolean {
        return this.settings.appearance.titlebar.mainMenuVisibility
            !== this.currentSettings.appearance.titlebar.mainMenuVisibility
            && this.currentSettings.appearance.titlebar.type === "Native";
    }

    public async browseForDotNetSdkDirectory() {
        const path = await this.browseForDirectory("Select .NET SDK Directory", this.settings.dotNetSdkDirectoryPath);
        if (path) this.settings.dotNetSdkDirectoryPath = path;
    }

    public async browseForScriptsDirectory() {
        const path = await this.browseForDirectory("Select Scripts Directory", this.settings.scriptsDirectoryPath);
        if (path) this.settings.scriptsDirectoryPath = path;
    }

    public async browseForPackageCacheDirectory() {
        const path = await this.browseForDirectory("Select Package Cache Directory", this.settings.packageCacheDirectoryPath);
        if (path) this.settings.packageCacheDirectoryPath = path;
    }

    public async openPackageCacheFolder() {
        await this.appService.openPackageCacheFolder();
    }

    public async purgePackageCache() {
        const confirmation = await this.dialogUtil.ask({
            title: "Purge Package Cache",
            message: "Delete every downloaded package? Packages your scripts reference are downloaded again the next time they run."
        });

        if (confirmation.value !== "OK") return;

        try {
            await this.packageService.purgePackageCache();
        } catch (ex) {
            this.logger.error("Error purging package cache", ex);
        }

        await this.loadCacheInfo();
    }

    private async browseForDirectory(title: string, defaultPath?: string): Promise<string | undefined> {
        const paths = await this.nativeDialogService.showFileSelectorDialog({
            title: title,
            directory: true,
            defaultPath: defaultPath || undefined,
        });

        return paths?.[0];
    }

    private async loadCacheInfo() {
        this.cacheSize = undefined;
        this.cachePackageCount = undefined;

        try {
            const info = await this.packageService.getPackageCacheInfo();
            this.cacheSize = Util.formatByteSize(info.sizeInBytes);
            this.cachePackageCount = info.packageCount;
        } catch (ex) {
            this.logger.error("Error getting package cache info", ex);
        }
    }

    private async loadUpdateStatus() {
        const current = (await this.appService.getIdentifier()).productVersion;
        this.appVersion = current;

        try {
            const latest = await this.appService.getLatestVersion();
            if (!latest) return;

            this.updateStatus = latest === current ? "latest" : `v${latest} available`;
        } catch (ex) {
            this.logger.debug("Could not check for the latest version", ex);
        }
    }
}
