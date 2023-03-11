import {DialogBase} from "@application/dialogs/dialog-base";
import {IDialogDom} from "@aurelia/runtime-html";
import {ILogger} from "aurelia";
import {AppDependencyCheckResult, IAppService} from "@domain";

export class AppDependenciesCheckDialog extends DialogBase {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
    public latestDotnetSdkVersion?: string;
    public dependencyCheckResult?: AppDependencyCheckResult;
    public loading = true;

    constructor(@IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger,
                @IAppService private readonly appService: IAppService) {
        super(dialogDom, logger);
    }

    public activate(dependencyCheckResult?: AppDependencyCheckResult) {
        const promise: Promise<AppDependencyCheckResult> = dependencyCheckResult
            ? Promise.resolve(dependencyCheckResult)
            : this.appService.checkDependencies();

        promise.then(result => {
            this.dependencyCheckResult = result;

            this.dotnetSdkMissing =
                this.dependencyCheckResult.dotNetSdkVersions.length === 0
                || !this.dependencyCheckResult.dotNetSdkVersions.some(v => v.startsWith("6"));

            this.latestDotnetSdkVersion = this.dependencyCheckResult.dotNetSdkVersions.length === 0
                ? undefined
                : [...this.dependencyCheckResult.dotNetSdkVersions].sort((a, b) => -1 * a.localeCompare(b))[0];

            this.dotnetEfCoreToolMissing = !this.dependencyCheckResult.dotNetEfToolVersion;

            this.loading = false;
        });
    }
}
