import {AppDependencyCheckResult, DotNetPathReport, IAppService, SemanticVersion} from "@application";
import {Dialog} from "@application/dialogs/dialog";

export class AppDependenciesCheckDialog extends Dialog<AppDependencyCheckResult> {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
    public report?: DotNetPathReport;

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
}
