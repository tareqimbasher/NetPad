import {DialogBase} from "@application/dialogs/dialog-base";
import {IDialogDom} from "@aurelia/runtime-html";
import {ILogger} from "aurelia";
import {AppDependencyCheckResult, IAppService} from "@domain";

export class AppDependenciesCheckDialog extends DialogBase {
    public dotnetSdkMissing = false;
    public dotnetEfCoreToolMissing = false;
    public dependencyCheckResult?: AppDependencyCheckResult;

    constructor(@IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger,
                @IAppService private readonly appService: IAppService) {
        super(dialogDom, logger);
    }

    public async activate(dependencyCheckResult?: AppDependencyCheckResult) {
        if (!dependencyCheckResult) {
            dependencyCheckResult = await this.appService.checkDependencies();
        }

        this.dependencyCheckResult = dependencyCheckResult;

        this.dotnetSdkMissing = !dependencyCheckResult.dotNetSdkVersion;
        this.dotnetEfCoreToolMissing = !dependencyCheckResult.dotNetEfToolVersion;
    }
}
