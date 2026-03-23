import {IDataConnectionViewComponent} from "./idata-connection-view-component";
import {DatabaseServerConnection} from "@application";
import {CommonServices} from "../common-services";

export interface IServerDatabasesLoadingOptions {
    requirementsToLoadAreMet: () => boolean;
}

export class ServerDatabasesComponent implements IDataConnectionViewComponent {
    public loadingDatabases = false;
    public databasesOnServer?: string[];

    constructor(
        private readonly connection: DatabaseServerConnection,
        private readonly commonServices: CommonServices,
        private readonly loadingOptions: IServerDatabasesLoadingOptions) {
    }

    public get validationError(): string | undefined {
        return undefined;
    }

    public async loadDatabases() {
        if (this.loadingDatabases) {
            return;
        }

        const canLoad = this.loadingOptions.requirementsToLoadAreMet();

        if (!canLoad) {
            this.databasesOnServer = undefined;
            return;
        }

        this.loadingDatabases = true;

        try {
            const databases = await this.commonServices.dataConnectionService.getDatabases(this.connection);
            this.databasesOnServer = databases.sort((a, b) => a.localeCompare(b));
        } finally {
            this.loadingDatabases = false;
        }
    }

    public selectAll() {
        if (!this.databasesOnServer) return;
        this.connection.selectedDatabaseNames = [...this.databasesOnServer];
    }

    public clearSelection() {
        this.connection.selectedDatabaseNames = [];
    }

    public toggleDatabase(dbName: string) {
        if (!this.connection.selectedDatabaseNames) {
            this.connection.selectedDatabaseNames = [];
        }

        const ix = this.connection.selectedDatabaseNames.indexOf(dbName);
        if (ix >= 0) {
            this.connection.selectedDatabaseNames.splice(ix, 1);
        } else {
            this.connection.selectedDatabaseNames.push(dbName);
        }
    }
}
