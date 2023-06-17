import {IContainer, ILogger} from "aurelia";
import {IDialogDom, IDialogService} from "@aurelia/dialog";
import {Version} from "@common/data/version";
import {DialogBase} from "@application/dialogs/dialog-base";
import {IAppService} from "@domain";
import {System} from "@common";

export interface IAppUpdateDialogModel {
    current: Version,
    latest: Version
}

export class AppUpdateDialog extends DialogBase {
    public versions?: IAppUpdateDialogModel;
    public loading = false;

    public get newerVersionExists() {
        return this.versions && this.versions.latest.greaterThan(this.versions.current);
    }

    constructor(@IAppService private readonly appService: IAppService,
                @IDialogDom dialogDom: IDialogDom,
                @ILogger logger: ILogger) {
        super(dialogDom, logger);
    }

    public activate(model: IAppUpdateDialogModel) {
        if (model) {
            this.versions = model;
            return;
        }

        this.loading = true;
        this.appService.getCurrentAndLatestVersions()
            .then(versions => {
                if (versions) this.versions = versions;
            })
            .catch(err => {
                this.logger.error("Error while getting versions", err);
            })
            .finally(() => {
                if (!this.versions) {
                    alert("Failed to check for updates. Please try again later.");
                    this.cancel();
                }

                this.loading = false;
            });
    }

    public async openLatestVersionPage() {
        await System.openUrlInBrowser("https://github.com/tareqimbasher/NetPad/releases/latest");
        await this.ok();
    }
}
