import {Constructable} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    DatabaseConnection,
    DatabaseServerConnection,
    DataConnection,
    DataConnectionType,
    IDataConnectionService,
    IWindowService,
    MariaDbDatabaseServerConnection,
    MsSqlServerDatabaseServerConnection,
    MySqlDatabaseServerConnection,
    PostgreSqlDatabaseServerConnection,
} from "@application";
import {WindowBase} from "@application/windows/window-base";
import {WindowParams} from "@application/windows/window-params";
import {Util} from "@common";
import {IDataConnectionView} from "./connection-views/idata-connection-view";
import {MssqlView} from "./connection-views/mssql/mssql-view";
import {PostgresqlView} from "./connection-views/postgresql/postgresql-view";
import {SqliteView} from "./connection-views/sqlite/sqlite-view";
import {MysqlView} from "./connection-views/mysql/mysql-view";
import {OracleView} from "./connection-views/oracle/oracle-view";
import {MariaDbView} from "./connection-views/mariadb/mariadb-view";
import {ServerView} from "./connection-views/server/server-view";
import {CommonServices} from "./connection-views/common-services";
import {INativeDialogService} from "@application/dialogs/inative-dialog-service";

const serverViewRegistry = new Map<DataConnectionType, Constructable<DatabaseServerConnection>>([
    ["MSSQLServer", MsSqlServerDatabaseServerConnection],
    ["PostgreSQL", PostgreSqlDatabaseServerConnection],
    ["MySQL", MySqlDatabaseServerConnection],
    ["MariaDB", MariaDbDatabaseServerConnection],
]);

const connectionViewRegistry = new Map<DataConnectionType, (conn: DataConnection | undefined, svc: CommonServices) => IDataConnectionView>([
    ["MSSQLServer", (c, s) => new MssqlView(c, s)],
    ["PostgreSQL", (c, s) => new PostgresqlView(c, s)],
    ["SQLite", (c, s) => new SqliteView(c, s)],
    ["MySQL", (c, s) => new MysqlView(c, s)],
    ["MariaDB", (c, s) => new MariaDbView(c, s)],
    ["Oracle", (c, s) => new OracleView(c, s)],
]);

export class Window extends WindowBase {
    public connectionView?: IDataConnectionView;
    public connectionType?: ConnectionType;
    public connectionTypes: ConnectionType[];

    public testingConnectionStatus?: undefined | "testing" | "success" | "fail";
    public testingConnectionFailureMessage?: string;
    public prohibitedNames: string[] = [];
    public connectionString = "";
    private nameField: HTMLInputElement;
    private readonly startupParams: ReturnType<Window["getStartupParams"]>;

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IWindowService private readonly windowService: IWindowService,
        @INativeDialogService private readonly nativeDialogService: INativeDialogService
    ) {
        super();

        const params = this.startupParams = this.getStartupParams();

        if (params.isServer) {
            document.title = params.createNew ? "New Database Server" : "Edit Database Server";
            this.connectionTypes = [
                {label: '<img src="/img/mssql.png" class="connection-type-logo"/> SQL Server', type: "MSSQLServer"},
                {
                    label: '<img src="/img/postgresql2.png" class="connection-type-logo"/> PostgreSQL',
                    type: "PostgreSQL"
                },
                {label: '<img src="/img/mysql.png" class="connection-type-logo"/> MySQL', type: "MySQL"},
                {label: '<img src="/img/mariadb.png" class="connection-type-logo"/> MariaDB', type: "MariaDB"},
            ];
        } else {
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
                {label: '<img src="/img/mysql.png" class="connection-type-logo"/> MySQL', type: "MySQL"},
                {label: '<img src="/img/mariadb.png" class="connection-type-logo"/> MariaDB', type: "MariaDB"},
                {label: '<img src="/img/sqlite.png" class="connection-type-logo"/> SQLite', type: "SQLite"},
                {label: '<img src="/img/oracle.png" class="connection-type-logo"/> Oracle', type: "Oracle"},
            ];
        }
    }

    private getStartupParams() {
        const dataConnectionId = WindowParams.get("data-connection-id");
        const copy = WindowParams.get("copy")?.toLowerCase() === "true";
        const isServer = WindowParams.get("is-server")?.toLowerCase() === "true";

        return {
            createNew: !dataConnectionId || copy,
            createCopy: copy,
            dataConnectionId: dataConnectionId,
            isServer: isServer
        }
    }

    public async binding() {
        if (this.startupParams.dataConnectionId) {
            const connection = await this.loadConnection(this.startupParams.dataConnectionId, this.startupParams.isServer);

            if (this.startupParams.createNew && this.startupParams.createCopy) {
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
            this._showConnectionStringAugment =
                (this.connectionView?.connection instanceof DatabaseConnection || this.connectionView?.connection instanceof DatabaseServerConnection)
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

        const commonServices: CommonServices = {
            dataConnectionService: this.dataConnectionService,
            nativeDialogService: this.nativeDialogService,
        }

        if (this.startupParams.isServer) {
            const ctor = serverViewRegistry.get(connectionType);
            return ctor ? new ServerView(ctor, connection, commonServices) : undefined;
        }

        const factory = connectionViewRegistry.get(connectionType);
        return factory?.(connection, commonServices);
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
            if ((connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection) && connection.port?.trim() === "") {
                connection.port = undefined;
            }

            await this.saveConnection(connection);
            await this.windowService.close();
        } catch (ex) {
            const errorMsg = ex instanceof Error ? ex.toString() : "Unknown error";
            alert("Could not save the connection: " + errorMsg);
            this.logger.error("Error while saving connection", ex);
        }
    }

    public async cancel() {
        await this.windowService.close();
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
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection | DatabaseServerConnection)?.host)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection | DatabaseServerConnection)?.port)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection | DatabaseServerConnection)?.userId)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection | DatabaseServerConnection)?.password)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection)?.databaseName)
    @watch<Window>(vm => (vm.connectionView?.connection as DatabaseConnection | DatabaseServerConnection)?.connectionStringAugment)
    private async updateConnectionString() {
        if (!this.connectionView) {
            this.connectionString = "";
            return;
        }

        this.connectionString = await this.dataConnectionService.getConnectionString(this.connectionView.connection);
    }

    private async loadConnection(id: string, isServer: boolean): Promise<DataConnection> {
        return isServer
            ? await this.dataConnectionService.getServer(id)
            : await this.dataConnectionService.get(id);
    }

    private async saveConnection(connection: DataConnection): Promise<void> {
        if (connection instanceof DatabaseServerConnection)
            await this.dataConnectionService.saveServer(connection);
        else
            await this.dataConnectionService.save(connection);
    }

}

class ConnectionType {
    public label: string;
    public type: DataConnectionType;
}
