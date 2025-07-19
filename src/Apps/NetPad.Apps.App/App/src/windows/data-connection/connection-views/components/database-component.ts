import {IDataConnectionViewComponent} from "./idata-connection-view-component";
import {DatabaseConnection} from "@application";
import {CommonServices} from "../common-services";

export interface IDatabaseComponentOptions {
    allowSelectDatabaseFile: boolean;
}

/**
 * Options to control if and when database names are loaded from server.
 */
export interface IDatabaseLoadingOptions {
    enabled: boolean;
    requirementsToLoadAreMet: () => boolean;
}

export class DatabaseComponent implements IDataConnectionViewComponent {
    public loadingDatabases = false;
    public databasesOnServer?: string[];

    constructor(
        private readonly connection: DatabaseConnection,
        private readonly commonServices: CommonServices,
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
                this.databasesOnServer = await this.commonServices.dataConnectionService.getDatabases(this.connection);
            } finally {
                this.loadingDatabases = false;
            }
        }
    }

    public async browseDatabaseFile() {
        const paths = await this.commonServices.nativeDialogService.showFileSelectorDialog({
            title: "Database file",
            multiple: false,
        });

        if (!paths || paths.length === 0) {
            return;
        }

        this.connection.databaseName = paths[0];
    }
}
