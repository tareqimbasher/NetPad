import {bindable, ILogger} from "aurelia";
import {
    AppDependencyCheckResult,
    AppIdentifier,
    IAppService,
    ISettingsService,
    Settings,
    ViewModelBase
} from "@application";
import {WindowParams} from "@application/windowing/window-params";

export class About extends ViewModelBase {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    public appId?: AppIdentifier;
    public dependencies?: AppDependencyCheckResult;
    public latestVersion?: string;
    public isCheckingForUpdate = false;
    public didCheckForUpdate = false;

    public readonly repositoryUrl = "https://github.com/tareqimbasher/NetPad";
    public readonly sponsorUrl = "https://github.com/sponsors/tareqimbasher";

    constructor(
        currentSettings: Settings,
        @IAppService private readonly appService: IAppService,
        @ISettingsService private readonly settingsService: ISettingsService,
        @ILogger logger: ILogger) {
        super(logger);
        this.currentSettings = currentSettings;
    }

    public binding() {
        void this.load();
    }

    public get shellName(): string {
        return WindowParams.shell;
    }

    public get sdkVersions(): string {
        return this.dependencies?.supportedDotNetSdkVersionsInstalled
            .map(v => v.version.string)
            .join(" · ") ?? "";
    }

    public get efToolVersion(): string {
        return this.dependencies?.dotNetEfToolVersion?.string ?? "not installed";
    }

    public get updateStatus(): "unknown" | "up-to-date" | "outdated" {
        if (!this.latestVersion || !this.appId) return "unknown";
        return this.latestVersion === this.appId.productVersion ? "up-to-date" : "outdated";
    }

    public async checkForUpdate() {
        this.isCheckingForUpdate = true;

        try {
            this.latestVersion = await this.appService.getLatestVersion() ?? undefined;
        } catch (ex) {
            this.logger.debug("Could not check for the latest version", ex);
        } finally {
            this.isCheckingForUpdate = false;
            this.didCheckForUpdate = true;
        }
    }

    public async openAppDataFolder() {
        await this.settingsService.showSettingsFile();
    }

    /** A paste-ready block for bug reports. */
    public async copyEnvironment() {
        const lines = [
            `NetPad ${this.appId?.productVersion}`,
            `Shell: ${this.shellName}`,
            `.NET runtime: ${this.dependencies?.dotNetRuntimeVersion}`,
            `.NET SDKs: ${this.sdkVersions}`,
            `EF Core tool: ${this.efToolVersion}`,
            `App data: ${this.appId?.appDataDirectoryPath}`,
        ];

        await navigator.clipboard.writeText(lines.join("\n"));
    }

    private async load() {
        this.appId = await this.appService.getIdentifier();

        try {
            this.dependencies = await this.appService.checkDependencies();
        } catch (ex) {
            this.logger.error("Error checking app dependencies", ex);
        }

        await this.checkForUpdate();
    }
}
