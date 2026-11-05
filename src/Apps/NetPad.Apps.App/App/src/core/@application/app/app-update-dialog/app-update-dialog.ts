import {Version} from "@common/data/version";
import {Dialog} from "@application/dialogs/dialog";
import {IAppService} from "@application";
import {ISystemService} from "@application/system/isystem-service";

export interface IAppUpdateDialogModel {
    current: Version,
    latest: Version
}

export class AppUpdateDialog extends Dialog<IAppUpdateDialogModel> {
    public loading = false;
    public failed = false;

    public get newerVersionExists() {
        return this.input && this.input.latest.greaterThan(this.input.current);
    }

    constructor(
        @IAppService private readonly appService: IAppService,
        @ISystemService private readonly systemService: ISystemService) {
        super();
    }

    public bound() {
        if (!this.input) {
            void this.check();
        }
    }

    public async tryAgain() {
        this.dialogDom.contentHost.focus();
        await this.check();
    }

    private async check() {
        this.loading = true;
        this.failed = false;

        try {
            const versions = await this.appService.getCurrentAndLatestVersions();
            if (versions) this.input = versions;
        } catch (err) {
            this.logger.error("Error while getting versions", err);
        } finally {
            this.failed = !this.input;
            this.loading = false;
        }
    }

    public async openLatestVersionPage() {
        const version = this.input?.latest.toString();
        const url = version
            ? `https://github.com/tareqimbasher/NetPad/releases/tag/v${version}`
            : "https://github.com/tareqimbasher/NetPad/releases/latest";
        this.systemService.openUrlInBrowser(url);
        await this.ok();
    }
}
