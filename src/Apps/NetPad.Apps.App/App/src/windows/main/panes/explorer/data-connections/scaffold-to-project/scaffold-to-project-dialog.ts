import {open} from "@tauri-apps/plugin-dialog";
import {Dialog} from "@application/dialogs/dialog";
import {
    DataConnection,
    DotNetFrameworkVersion,
    errorMessageOf,
    IAppService,
    IDataConnectionService,
    ISession
} from "@application";

interface IScaffoldToProjectDialogOptions {
    connectionId: string
}

export class ScaffoldToProjectDialog extends Dialog<IScaffoldToProjectDialogOptions> {
    public connection: DataConnection;
    public projectDirectory?: string;
    public availableFrameworkVersions: DotNetFrameworkVersion[] = [];
    public selectedDotNetFrameworkVersion?: DotNetFrameworkVersion;
    public isWorking = false;
    public error?: string;

    constructor(
        @IAppService private readonly appService: IAppService,
        @ISession private readonly session: ISession,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
    ) {
        super();
    }

    public async bound() {
        const connectionId = this.input?.connectionId;
        if (!connectionId) {
            return;
        }

        this.connection = await this.dataConnectionService.get(connectionId);

        this.availableFrameworkVersions = await this.appService.getAvailableDotNetSdkVersions();

        if (this.session.active) {
            this.selectedDotNetFrameworkVersion = this.session.active.script.config.targetFrameworkVersion;
        }
    }

    public async showDirPicker() {
        const path = await open({
            title: "Project Directory",
            directory: true,
            multiple: false,
            canCreateDirectories: true,
        });

        if (path) {
            this.projectDirectory = path;
        }
    }

    public async scaffold() {
        this.error = undefined;

        if (!this.projectDirectory) {
            return;
        }

        this.isWorking = true;

        try {
            await this.dataConnectionService.scaffoldToProject(
                this.connection.id,
                this.projectDirectory,
                this.selectedDotNetFrameworkVersion);

            await this.ok(this.projectDirectory);
        } catch (e) {
            this.logger.error(e);
            this.error = errorMessageOf(e, "An error occurred creating the project. Check the log file.");
        } finally {
            this.isWorking = false;
        }
    }

    private async copyErrorToClipboard() {
        if (this.error) {
            await navigator.clipboard.writeText(this.error);
        }
    }
}
