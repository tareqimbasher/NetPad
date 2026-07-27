import {Constructable} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    DatabaseConnection,
    DatabaseServerConnection,
    DataConnection,
    DataConnectionType,
    EntityFrameworkDatabaseConnection,
    EntityFrameworkDatabaseServerConnection,
    IDataConnectionService,
    IWindowService,
    MariaDbDatabaseServerConnection,
    MsSqlServerDatabaseServerConnection,
    MySqlDatabaseServerConnection,
    PostgreSqlDatabaseServerConnection,
    ValueSelectOption,
} from "@application";
import {WindowBase} from "@application/windowing/window-base";
import {WindowParams} from "@application/windowing/window-params";
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

const providers: (ValueSelectOption & {value: DataConnectionType})[] = [
    {value: "PostgreSQL", label: "PostgreSQL", detail: "npgsql", icon: "database"},
    {value: "MSSQLServer", label: "SQL Server", detail: "microsoft.data", icon: "database"},
    {value: "SQLite", label: "SQLite", detail: "file-based", icon: "database"},
    {value: "MySQL", label: "MySQL", detail: "pomelo", icon: "database"},
    {value: "MariaDB", label: "MariaDB", detail: "pomelo", icon: "database"},
    {value: "Oracle", label: "Oracle", detail: "oracle.ef", icon: "database"},
];

// Provider types that are servers that can host multiple databases.
const serverProviderTypes: DataConnectionType[] = ["MSSQLServer", "PostgreSQL", "MySQL", "MariaDB"];

type TestStatus = undefined | "testing" | "success" | "fail";

export class Window extends WindowBase {
    public connectionView?: IDataConnectionView;
    public providerType?: DataConnectionType;
    public readonly providerOptions: ValueSelectOption[];

    public testingConnectionStatus: TestStatus;
    public testingConnectionFailureMessage?: string;
    public testDurationText?: string;
    public testServerVersion?: string;
    public prohibitedNames: string[] = [];
    public connectionString = "";
    public showConnectionStringAugment = false;
    public showScaffoldingOptions = false;
    public nameTouched = false;
    public managingServerName?: string;

    private pendingName?: string;
    private pendingContainsProductionData = false;
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
            this.providerOptions = providers.filter(p => serverProviderTypes.includes(p.value));
        } else {
            document.title = params.createNew ? "New Data Connection" : "Edit Data Connection";
            this.providerOptions = providers;
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

            this.connectionView = this.createNewConnectionView(connection.type, connection);
            this.providerType = connection.type;
            this.showConnectionStringAugment = !!this.connectionStringAugment;

            await this.loadManagingServerName();
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

    public get autoOpenProviderMenu(): boolean {
        return this.startupParams.createNew && !this.startupParams.dataConnectionId;
    }

    public get saveButtonText(): string {
        return this.startupParams.isServer ? "Save server" : "Save connection";
    }

    public get name(): string | undefined {
        return this.connectionView ? this.connectionView.connection.name : this.pendingName;
    }

    public set name(value: string | undefined) {
        if (this.connectionView) {
            this.connectionView.connection.name = value as string;
        } else {
            this.pendingName = value;
        }
    }

    public get containsProductionData(): boolean {
        const connection = this.connectionView?.connection;
        return connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection
            ? connection.containsProductionData
            : this.pendingContainsProductionData;
    }

    public set containsProductionData(value: boolean) {
        const connection = this.connectionView?.connection;
        if (connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection) {
            connection.containsProductionData = value;
        } else {
            this.pendingContainsProductionData = value;
        }
    }

    public get connectionStringAugment(): string | undefined {
        const connection = this.connectionView?.connection;
        return connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection
            ? connection.connectionStringAugment
            : undefined;
    }

    public set connectionStringAugment(value: string | undefined) {
        const connection = this.connectionView?.connection;
        if (connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection) {
            connection.connectionStringAugment = value;
        }
    }

    public get isConnectionValid() {
        return !!this.providerType
            && !!this.connectionView
            && this.isNameValid()
            && !this.connectionView.validationError;
    }

    public isNameValid() {
        const name = this.name;
        return !!name && this.prohibitedNames.indexOf(name) < 0;
    }

    public get nameError(): string | undefined {
        const name = this.name;

        if (!name) {
            return this.nameTouched ? "A name is required." : undefined;
        }

        return this.prohibitedNames.indexOf(name) >= 0
            ? `A connection named “${name}” already exists.`
            : undefined;
    }

    public get canHideConnectionStringAugment(): boolean {
        return !this.connectionStringAugment;
    }

    public get scaffoldingOptionsConnection(): EntityFrameworkDatabaseConnection | EntityFrameworkDatabaseServerConnection | undefined {
        const connection = this.connectionView?.connection;
        return connection instanceof EntityFrameworkDatabaseConnection || connection instanceof EntityFrameworkDatabaseServerConnection
            ? connection
            : undefined;
    }

    public get isScaffoldingManagedByServer(): boolean {
        const connection = this.connectionView?.connection;
        return connection instanceof DatabaseConnection && !!connection.serverId;
    }

    public get testSuccessText(): string {
        const provider = this.providerOptions.find(o => o.value === this.providerType)?.label;
        const version = this.testServerVersion ? `${provider} ${this.testServerVersion}` : undefined;

        return ["connected", this.testDurationText, version].filter(p => !!p).join(" · ");
    }

    public async testConnection() {
        if (!this.connectionView || !this.isConnectionValid) {
            return;
        }

        this.testingConnectionStatus = "testing";
        this.testingConnectionFailureMessage = undefined;
        this.testServerVersion = undefined;
        this.testDurationText = undefined;

        const startedAt = performance.now();

        try {
            const result = await this.dataConnectionService.test(this.connectionView.connection);
            this.testDurationText = Window.formatDuration(performance.now() - startedAt);
            this.testingConnectionStatus = result.success ? "success" : "fail";
            this.testingConnectionFailureMessage = result.message;
            this.testServerVersion = result.serverVersion;
        } catch (ex) {
            this.testDurationText = Window.formatDuration(performance.now() - startedAt);
            this.testingConnectionStatus = "fail";
            if (ex instanceof Error) {
                this.testingConnectionFailureMessage = ex.toString();
            }
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

    @watch<Window>(vm => vm.providerType)
    private providerTypeChanged() {
        if (this.connectionView?.connection.type === this.providerType) {
            return;
        }

        this.connectionView = this.createNewConnectionView(this.providerType, this.connectionView?.connection);
        this.testingConnectionStatus = undefined;
    }

    private createNewConnectionView(connectionType: DataConnectionType | undefined, connection: DataConnection | undefined): IDataConnectionView | undefined {
        if (!connectionType) {
            return undefined;
        }

        const commonServices: CommonServices = {
            dataConnectionService: this.dataConnectionService,
            nativeDialogService: this.nativeDialogService,
        }

        const view = this.startupParams.isServer
            ? this.createNewServerView(connectionType, connection, commonServices)
            : connectionViewRegistry.get(connectionType)?.(connection, commonServices);

        if (view && !connection) {
            this.carryOverPendingValues(view);
        }

        return view;
    }

    private createNewServerView(connectionType: DataConnectionType, connection: DataConnection | undefined, commonServices: CommonServices) {
        const ctor = serverViewRegistry.get(connectionType);
        return ctor ? new ServerView(ctor, connection, commonServices) : undefined;
    }

    /** What the empty state collected before a provider existed to hold it. */
    private carryOverPendingValues(view: IDataConnectionView) {
        if (this.pendingName) {
            view.connection.name = this.pendingName;
        }

        if (view.connection instanceof DatabaseConnection || view.connection instanceof DatabaseServerConnection) {
            view.connection.containsProductionData = this.pendingContainsProductionData;
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

    private async loadManagingServerName() {
        const connection = this.connectionView?.connection;
        if (!(connection instanceof DatabaseConnection) || !connection.serverId) {
            return;
        }

        try {
            this.managingServerName = (await this.dataConnectionService.getServer(connection.serverId)).name;
        } catch (ex) {
            this.logger.error("Could not load the server this connection belongs to", ex);
        }
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

    private static formatDuration(milliseconds: number): string {
        return milliseconds < 1000
            ? `${Math.round(milliseconds)} ms`
            : `${(milliseconds / 1000).toFixed(1)} s`;
    }
}
