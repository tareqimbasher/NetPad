import {AppDependencyCheckResult, IAppService, SemanticVersion} from "@application";
import {Dialog} from "@application/dialogs/dialog";

export class AppDependenciesCheckDialog extends Dialog<AppDependencyCheckResult> {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
    public latestDotnetSdkVersion?: SemanticVersion;
    public loading = true;

    constructor(@IAppService private readonly appService: IAppService) {
        super();
    }

    public bound() {
        const promise: Promise<AppDependencyCheckResult> = this.input
            ? Promise.resolve(this.input)
            : this.appService.checkDependencies();

        promise.then(result => {
            this.input = result;

            this.dotnetSdkMissing = this.input.supportedDotNetSdkVersionsInstalled.length === 0;

            this.latestDotnetSdkVersion = this.input.supportedDotNetSdkVersionsInstalled.length === 0
                ? undefined
                : [...this.input.supportedDotNetSdkVersionsInstalled]
                    .sort((a, b) => b.major - a.major)[0];

            this.dotnetEfCoreToolMissing = !this.input.isSupportedDotNetEfToolInstalled;

            this.loading = false;
        });
    }
}
