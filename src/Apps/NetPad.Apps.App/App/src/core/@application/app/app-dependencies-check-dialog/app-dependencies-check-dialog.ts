import {AppDependencyCheckResult, DotNetPathReport, DotNetSdkVersion, IAppService} from "@application";
import {Dialog} from "@application/dialogs/dialog";

export class AppDependenciesCheckDialog extends Dialog<AppDependencyCheckResult> {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
    public report?: DotNetPathReport;
    public sdkGroups: { path: string; sdks: DotNetSdkVersion[] }[] = [];
    public supportedVersionStrings = new Set<string>();

    private loadingCount = 0;

    public get loading(): boolean {
        return this.loadingCount > 0;
    }

    constructor(@IAppService private readonly appService: IAppService) {
        super();
    }

    public bound() {
        this.loadingCount++;
        const promise: Promise<AppDependencyCheckResult> = this.input
            ? Promise.resolve(this.input)
            : this.appService.checkDependencies();

        promise.then(result => {
            this.input = result;

            this.dotnetSdkMissing = this.input.supportedDotNetSdkVersionsInstalled.length === 0;
            this.dotnetEfCoreToolMissing = !this.input.isSupportedDotNetEfToolInstalled;

            this.supportedVersionStrings = new Set(
                this.input.supportedDotNetSdkVersionsInstalled.map(s => s.version.string)
            );

            this.sdkGroups = this.groupSdksByInstallation(this.input.dotNetSdkVersions);
        }).finally(() => {
            this.loadingCount--;
        });

        this.loadingCount++;
        this.appService.getDotNetPathReport().then(report => {
            this.report = report;
        }).finally(() => {
            this.loadingCount--;
        });
    }

    private groupSdksByInstallation(sdks: DotNetSdkVersion[]): { path: string; sdks: DotNetSdkVersion[] }[] {
        const groups = new Map<string, DotNetSdkVersion[]>();

        for (const sdk of sdks) {
            const path = sdk.dotNetRootDirectory || "Unknown";
            if (!groups.has(path)) {
                groups.set(path, []);
            }
            groups.get(path)!.push(sdk);
        }

        return Array.from(groups.entries()).map(([path, sdks]) => ({path, sdks}));
    }
}
