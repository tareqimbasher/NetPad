import {watch} from "@aurelia/runtime-html";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";
import {DatabaseServerConnection} from "@application";
import {Util} from "@common";
import {CommonServices} from "../common-services";

export interface IServerDatabasesLoadingOptions {
    requirementsToLoadAreMet: () => boolean;
}

export class ServerDatabasesComponent implements IDataConnectionViewComponent {
    public loadingDatabases = false;
    public databasesOnServer?: string[];
    public loadFailed = false;

    private readonly scheduleLoadDatabases = Util.debounceAsync(this, () => this.loadDatabases(), 400);

    constructor(
        private readonly connection: DatabaseServerConnection,
        private readonly commonServices: CommonServices,
        private readonly loadingOptions: IServerDatabasesLoadingOptions) {
    }

    public binding() {
        this.scheduleLoadDatabases();
    }

    public get validationError(): string | undefined {
        return undefined;
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

    @watch<ServerDatabasesComponent>(vm => vm.connection.host)
    @watch<ServerDatabasesComponent>(vm => vm.connection.port)
    @watch<ServerDatabasesComponent>(vm => vm.connection.userId)
    @watch<ServerDatabasesComponent>(vm => vm.connection.password)
    private serverChanged() {
        this.databasesOnServer = undefined;
        this.loadFailed = false;
        this.scheduleLoadDatabases();
    }

    private async loadDatabases() {
        if (!this.loadingOptions.requirementsToLoadAreMet()) {
            this.databasesOnServer = undefined;
            this.loadFailed = false;
            return;
        }

        this.loadingDatabases = true;

        try {
            const databases = await this.commonServices.dataConnectionService.getDatabases(this.connection);
            this.databasesOnServer = databases.sort((a, b) => a.localeCompare(b));
            this.loadFailed = false;
        } catch {
            this.databasesOnServer = undefined;
            this.loadFailed = true;
        } finally {
            this.loadingDatabases = false;
        }
    }
}
