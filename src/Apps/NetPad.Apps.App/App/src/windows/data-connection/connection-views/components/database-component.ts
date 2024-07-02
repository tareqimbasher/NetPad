import {observable} from "@aurelia/runtime";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";
import {DatabaseConnection, IDataConnectionService} from "@application";

export interface IDatabaseComponentOptions {
    allowSelectDatabaseFile: boolean;
}

/**
 * Options to control if and when database names are loaded from server.
 */
export interface IDatabaseLoadingOptions {
    enabled: boolean;
    requirementsToLoadAreMet: () => boolean;
    dataConnectionService: IDataConnectionService
}

export class DatabaseComponent implements IDataConnectionViewComponent {
    public loadingDatabases = false;
    public databasesOnServer?: string[];

    private browseInput: HTMLInputElement;
    @observable public browsedFile: FileList;

    constructor(
        private readonly connection: DatabaseConnection,
        private readonly options?: IDatabaseComponentOptions,
        private readonly dbLoadingOptions?: IDatabaseLoadingOptions) {

        if (!options) this.options = {allowSelectDatabaseFile: false};
    }

    public get validationError(): string | undefined {
        return !this.connection.databaseName ? "The Database is required." : undefined;
    }

    private async loadDatabases() {
        if (this.loadingDatabases || !this.dbLoadingOptions) {
            return;
        }

        const canLoad = this.dbLoadingOptions.enabled && this.dbLoadingOptions.requirementsToLoadAreMet();

        if (!canLoad) {
            this.databasesOnServer = undefined;
            return;
        }

        if (!this.databasesOnServer || !this.databasesOnServer.length) {
            this.loadingDatabases = true;

            try {
                this.databasesOnServer = await this.dbLoadingOptions.dataConnectionService.getDatabases(this.connection);
            } finally {
                this.loadingDatabases = false;
            }
        }
    }

    private browsedFileChanged(newValue: FileList) {
        if (!newValue || newValue.length === 0) {
            return;
        }

        const file = newValue.item(0);

        this.connection.databaseName = file?.path;

        // Clear file input element so if user selects a file, removes it, then re-selects it
        // the change is observed
        this.browseInput.value = "";
    }
}
