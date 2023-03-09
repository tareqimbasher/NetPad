import {DialogBase} from "@application/dialogs/dialog-base";
import {IDialogDom} from "@aurelia/runtime-html";
import {ILogger} from "aurelia";
import {AppDependencyCheckResult, IAppService} from "@domain";

export class AppDependenciesCheckDialog extends DialogBase {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
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

        promise.then(r => {
            this.dependencyCheckResult = r;
            this.dotnetSdkMissing = !this.dependencyCheckResult.dotNetSdkVersion;
            this.dotnetEfCoreToolMissing = !this.dependencyCheckResult.dotNetEfToolVersion;
            this.loading = false;
        });
    }
}
