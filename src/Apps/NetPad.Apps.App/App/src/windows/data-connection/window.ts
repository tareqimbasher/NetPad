import {watch} from "@aurelia/runtime-html";
import {DatabaseConnection, DataConnection, DataConnectionType, IDataConnectionService,} from "@application";
import {WindowBase} from "@application/windows/window-base";
import {System, Util} from "@common";
import {IDataConnectionView} from "./connection-views/idata-connection-view";
import {MssqlView} from "./connection-views/mssql/mssql-view";
import {PostgresqlView} from "./connection-views/postgresql/postgresql-view";
import {SqliteView} from "./connection-views/sqlite/sqlite-view";

export class Window extends WindowBase {
    public connectionView?: IDataConnectionView;
    public connectionType?: ConnectionType;
    public connectionTypes: ConnectionType[];

    public testingConnectionStatus?: undefined | "testing" | "success" | "fail";
    public testingConnectionFailureMessage?: string;
    public prohibitedNames: string[] = [];
    public connectionString = "";
    private nameField: HTMLInputElement;

    constructor(
        private readonly startupOptions: URLSearchParams,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService
    ) {
        super();

        const params = this.getStartupParams();
        document.title = params.createNew ? "New Data Connection" : "Edit Data Connection";

        this.connectionTypes = [
            {
                label: '<img src="/img/mssql.png" class="connection-type-logo"/> Microsoft SQL Server',
                type: "MSSQLServer"
            },
            {
                label: '<img src="/img/postgresql2.png" class="connection-type-logo"/> PostgreSQL',
                type: "PostgreSQL"
            },
        ];

        // Until we implement a way to add a SQLite file in the browser, this option will only be available in Electron app
        if (System.isRunningInElectron()) {
            this.connectionTypes.push({
                label: '<img src="/img/sqlite.png" class="connection-type-logo"/> SQLite',
                type: "SQLite"
            });
        }
    }

    private getStartupParams() {
        const dataConnectionId = this.startupOptions.get("data-connection-id");
        const copy = this.startupOptions.get("copy")?.toLowerCase() === "true";

        return {
            createNew: !dataConnectionId || copy,
            createCopy: copy,
            dataConnectionId: dataConnectionId
        }
    }

    public async binding() {
        const params = this.getStartupParams();

        if (params.dataConnectionId) {
            const connection = await this.dataConnectionService.get(params.dataConnectionId);

            if (params.createNew && params.createCopy) {
                connection.id = Util.newGuid();
                connection.name += " - Copy";
            }

            this.connectionType = this.connectionTypes.find(c => c.type == connection.type);

            this.connectionView = this.createNewConnectionView(this.connectionType?.type, connection);

            this.updateConnectionString();
        }

        const prohibitedNames = await this.dataConnectionService.getAllNames();

        // Remove the name of the connection being edited
        if (this.connectionView?.connection.name) {
            const ix = prohibitedNames.indexOf(this.connectionView.connection.name);
            if (ix >= 0) {
                prohibitedNames.splice(ix, 1);
            }
        }

        this.prohibitedNames = prohibitedNames;
    }

    public get isConnectionValid() {
        const genericChecks = !!this.connectionType
            && !!this.connectionView
            && this.isNameValid();

        return genericChecks && this.connectionView && !this.connectionView.validationError;
    }

    public isNameValid() {
        if (!this.connectionView || !this.connectionView.connection.name) {
            return false;
        }

        return this.prohibitedNames.indexOf(this.connectionView.connection.name) < 0;
    }


    private _showConnectionStringAugment = false;
    public get showConnectionStringAugment() {
        if (!this._showConnectionStringAugment) {
            this._showConnectionStringAugment = this.connectionView?.connection instanceof DatabaseConnection
                && !!this.connectionView.connection.connectionStringAugment;
        }

        return this._showConnectionStringAugment;
    }

    public set showConnectionStringAugment(value) {
        this._showConnectionStringAugment = value;
    }


    public setConnectionType(connectionType: ConnectionType) {
        if (this.testingConnectionStatus === "testing") {
            return;
        }

        if (this.connectionView?.connection.type === connectionType.type) {
            return;
        }

        this.connectionType = connectionType;

        this.connectionView = this.createNewConnectionView(this.connectionType.type, this.connectionView?.connection);
    }

    private createNewConnectionView(connectionType: DataConnectionType | undefined, connection: DataConnection | undefined): IDataConnectionView | undefined {
        if (!connectionType) {
            return undefined;
        }

        if (connectionType === "MSSQLServer") {
            return new MssqlView(connection, this.dataConnectionService);
        }

        if (connectionType === "PostgreSQL") {
            return new PostgresqlView(connection, this.dataConnectionService);
        }

        if (connectionType === "SQLite") {
            return new SqliteView(connection, this.dataConnectionService);
        }

        return undefined;
    }

    public async testConnection() {
        if (!this.connectionType || !this.connectionView) {
            alert("Configure the connection first.");
            return;
        }

        const validationError = this.connectionView.validationError;
        if (validationError) {
            alert(validationError);
            return;
        }

        this.testingConnectionStatus = "testing";

        try {
            const result = await this.dataConnectionService.test(this.connectionView.connection);
            this.testingConnectionStatus = result.success ? "success" : "fail";
            this.testingConnectionFailureMessage = result.message;
        } catch (ex) {
            this.testingConnectionStatus = "fail";
            if (ex instanceof Error)
                this.testingConnectionFailureMessage = ex.toString();
            this.logger.error("Error while testing connection", ex);
        }
    }

    public async save() {
        if (!this.isConnectionValid || !this.connectionView) {
            return;
        }

        const connection = this.connectionView.connection;

        try {
            if (connection instanceof DatabaseConnection && connection.port?.trim() === "") {
                connection.port = undefined;
            }

            await this.dataConnectionService.save(connection);
            window.close();
        } catch (ex) {
            const errorMsg = ex instanceof Error ? ex.toString() : "Unknown error";
            alert("Could not save the connection: " + errorMsg);
            this.logger.error("Error while saving connection", ex);
        }
    }

    public cancel() {
        window.close();
    }

    @watch<Window>(vm => vm.connectionView?.connection.name)
    private connectionNameChanged() {
        if (!this.connectionView?.connection.name) {
            this.nameField.parentElement?.classList.remove("was-validated");
            return;
        }

        this.nameField.parentElement?.classList.add("was-validated");

        if (!this.isNameValid()) {
            this.nameField.classList.replace("is-valid", "is-invalid");
            this.nameField.setCustomValidity("Unique name");
        } else {
            this.nameField.classList.replace("is-invalid", "is-valid");
            this.nameField.setCustomValidity("");
        }
    }

    @watch<Window>(vm => vm.connectionView?.connection.type)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.host)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.port)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.userId)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.password)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.databaseName)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.connectionStringAugment)
    private async updateConnectionString() {
        if (!this.connectionView) {
            this.connectionString = "";
            return;
        }

        this.connectionString = await this.dataConnectionService.getConnectionString(this.connectionView.connection);
    }
}

class ConnectionType {
    public label: string;
    public type: DataConnectionType;
}
