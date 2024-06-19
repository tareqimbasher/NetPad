import {Dialog} from "../dialog";
import {IAppService, SemanticVersion} from "@domain";
import {System} from "@common";

export interface IAppUpdateDialogModel {
    current: SemanticVersion,
    latest: SemanticVersion
}

export class AppUpdateDialog extends Dialog<IAppUpdateDialogModel> {
    public loading = false;

    public get newerVersionExists() {
        return this.input && (
            this.input.latest.major > this.input.current.major
            || this.input.latest.minor > this.input.current.minor
            || this.input.latest.patch > this.input.current.patch
        );
    }

    constructor(@IAppService private readonly appService: IAppService) {
        super();
    }

    public bound() {
        if (this.input) {
            return;
        }

        this.loading = true;
        this.appService.getCurrentAndLatestVersions()
            .then(versions => {
                if (versions) {
                    this.input = versions;
                }
            })
            .catch(err => {
                this.logger.error("Error while getting versions", err);
            })
            .finally(() => {
                if (!this.input) {
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
