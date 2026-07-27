import {watch} from "@aurelia/runtime-html";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";
import {DatabaseConnection, ValueSelectOption} from "@application";
import {Util} from "@common";
import {CommonServices} from "../common-services";

export interface IConnectionDatabaseComponentOptions {
    allowSelectDatabaseFile: boolean;
}

/**
 * Options to control if and when database names are loaded from server.
 */
export interface IConnectionDatabaseLoadingOptions {
    enabled: boolean;
    requirementsToLoadAreMet: () => boolean;
}

export class ConnectionDatabaseComponent implements IDataConnectionViewComponent {
    public loadingDatabases = false;
    public databasesOnServer?: string[];
    public readonly touched = {databaseName: false};

    private readonly scheduleLoadDatabases = Util.debounceAsync(this, () => this.loadDatabases(), 400);

    constructor(
        private readonly connection: DatabaseConnection,
        private readonly commonServices: CommonServices,
        private readonly options?: IConnectionDatabaseComponentOptions,
        private readonly dbLoadingOptions?: IConnectionDatabaseLoadingOptions) {

        if (!options) this.options = {allowSelectDatabaseFile: false};
    }

    public binding() {
        this.scheduleLoadDatabases();
    }

    public get validationError(): string | undefined {
        return !this.connection.databaseName ? "The Database is required." : undefined;
    }

    public get isFileBased(): boolean {
        return !this.dbLoadingOptions?.enabled;
    }

    public get databaseError(): string | undefined {
        if (!this.touched.databaseName || this.connection.databaseName) {
            return undefined;
        }

        return this.isFileBased ? "A database file is required." : "A database is required.";
    }

    public get databaseOptions(): ValueSelectOption[] {
        return this.databasesOnServer?.map(name => ({value: name, label: name})) ?? [];
    }

    public get databasesHint(): string | undefined {
        const loaded = this.databasesOnServer?.length;
        if (!loaded || this.loadingDatabases) {
            return undefined;
        }

        const others = this.databasesOnServer?.filter(d => d !== this.connection.databaseName).length ?? 0;

        return others === loaded ? `· ${loaded} loaded` : `· ${others} more loaded`;
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

    @watch<ConnectionDatabaseComponent>(vm => vm.connection.host)
    @watch<ConnectionDatabaseComponent>(vm => vm.connection.port)
    @watch<ConnectionDatabaseComponent>(vm => vm.connection.userId)
    @watch<ConnectionDatabaseComponent>(vm => vm.connection.password)
    private serverChanged() {
        this.databasesOnServer = undefined;
        this.scheduleLoadDatabases();
    }

    private async loadDatabases() {
        if (!this.dbLoadingOptions?.enabled || !this.dbLoadingOptions.requirementsToLoadAreMet()) {
            this.databasesOnServer = undefined;
            return;
        }

        this.loadingDatabases = true;

        try {
            const databases = await this.commonServices.dataConnectionService.getDatabases(this.connection);
            this.databasesOnServer = databases.sort((a, b) => a.localeCompare(b));
        } catch {
            this.databasesOnServer = undefined;
        } finally {
            this.loadingDatabases = false;
        }
    }
}
