import {Version} from "@common/data/version";
import {Dialog} from "@application/dialogs/dialog";
import {IAppService} from "@application";
import {ITranslationService} from "@application/i18n/itranslation-service";
import {System} from "@common";

export interface IAppUpdateDialogModel {
    current: Version,
    latest: Version
}

export class AppUpdateDialog extends Dialog<IAppUpdateDialogModel> {
    public loading = false;

    public get newerVersionExists() {
        return this.input && this.input.latest.greaterThan(this.input.current);
    }

    constructor(@IAppService private readonly appService: IAppService,
                @ITranslationService private readonly translation: ITranslationService) {
        super();
    }

    public bound() {
        if (this.input) {
            return;
        }

        this.loading = true;
        this.appService.getCurrentAndLatestVersions()
            .then(versions => {
                if (versions) this.input = versions;
            })
            .catch(err => {
                this.logger.error("Error while getting versions", err);
            })
            .finally(() => {
                if (!this.input) {
                    alert(this.translation.t("ui.failed-check-updates"));
                    this.cancel();
                }

                this.loading = false;
            });
    }

    public async openLatestVersionPage() {
        const version = this.input?.latest.toString();
        const url = version
            ? `https://github.com/tareqimbasher/NetPad/releases/tag/v${version}`
            : "https://github.com/tareqimbasher/NetPad/releases/latest";
        await System.openUrlInBrowser(url);
        await this.ok();
    }
}
