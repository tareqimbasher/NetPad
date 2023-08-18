import {
    DatabaseConnection,
    DataConnection,
    DataConnectionType,
    IDataConnectionService,
    MsSqlServerDatabaseConnection,
    PostgreSqlDatabaseConnection
} from "@domain";
import {Util} from "@common";
import {Constructable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {WindowBase} from "@application/windows/window-base";

export class Window extends WindowBase {
    public connection?: DataConnection;
    public connectionType?: ConnectionType;
    public connectionTypes: ConnectionType[] = [
        {
            label: '<img src="/img/mssql.png" class="connection-type-logo"/> Microsoft SQL Server',
            type: "MSSQLServer"
        },
        {
            label: '<img src="/img/postgresql2.png" class="connection-type-logo"/> PostgreSQL',
            type: "PostgreSQL"
        },
    ];

    public authType: "none" | "userAndPassword" = "userAndPassword";
    public testingConnectionStatus?: undefined | "testing" | "success" | "fail";
    public testingConnectionFailureMessage?: string;
    public loadingDatabases = false;
    public databasesOnServer?: string[];
    public prohibitedNames: string[] = [];
    public connectionString = "";
    private nameField: HTMLInputElement;
    private unprotectedPassword?: string;
    private readonly logger: ILogger;

    constructor(
        private readonly startupOptions: URLSearchParams,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(Window));

        const createNew = !this.startupOptions.get("data-connection-id");
        document.title = createNew ? "New Data Connection" : "Edit Data Connection";
    }

    public async binding() {
        const dataConnectionId = this.startupOptions.get("data-connection-id");
        if (dataConnectionId) {
            const connection = await this.dataConnectionService.get(dataConnectionId);

            this.connectionType = this.connectionTypes.find(c => c.type == connection.type);

            if (connection instanceof DatabaseConnection && (!!connection.userId || !!connection.password)) {
                this.authType = "userAndPassword";
            }

            this.connection = connection;

            this.updateConnectionString();
        }

        const existingNames = await this.dataConnectionService.getAllNames();

        // Remove the name of the connection being edited
        if (this.connection?.name) {
            const ix = existingNames.indexOf(this.connection.name);
            if (ix >= 0) {
                existingNames.splice(ix, 1);
            }
        }

        this.prohibitedNames = existingNames;
    }

    public get isConnectionValid() {
        const genericChecks = !!this.connectionType
            && !!this.connection
            && this.isNameValid();

        if (genericChecks && this.connection instanceof DatabaseConnection) {
            return !!this.connection.host
                && !!this.connection.databaseName
                && (this.authType !== "userAndPassword" || (!!this.connection.userId && !!this.connection.password));
        }

        return genericChecks;
    }

    public isNameValid() {
        if (!this.connection?.name) {
            return true;
        }

        return this.prohibitedNames.indexOf(this.connection.name) < 0;
    }

    public setConnectionType(connectionType: ConnectionType) {
        if (this.testingConnectionStatus === "testing") {
            return;
        }

        if (this.connection?.type === connectionType.type) {
            return;
        }

        this.connectionType = connectionType;

        let concereteType: Constructable;

        if (connectionType.type === "MSSQLServer") {
            concereteType = MsSqlServerDatabaseConnection;
        } else if (connectionType.type === "PostgreSQL") {
            concereteType = PostgreSqlDatabaseConnection;
        } else {
            this.connection = undefined;
            return;
        }

        const newConnection = new concereteType() as DatabaseConnection;

        if (this.connection) {
            newConnection.init(this.connection);
        }

        newConnection.id = this.connection?.id || Util.newGuid();
        newConnection.type = connectionType.type;

        if (!newConnection.name) newConnection.name = "@localhost";
        if (!newConnection.host) newConnection.host = "localhost";

        this.connection = newConnection;
    }

    public async testConnection() {
        if (!this.connectionType || !this.connection) {
            alert("Configure the connection first.");
            return;
        }

        if (this.connection instanceof DatabaseConnection) {
            if (!this.connection.host) {
                alert("The Host is required.");
                return;
            }

            if (!this.connection.databaseName) {
                alert("The Database is required.");
                return;
            }

            if (this.authType === "userAndPassword") {
                if (!this.connection.userId) {
                    alert("The User is required.");
                    return;
                }

                if (!this.connection.password) {
                    alert("The Password is required.");
                    return;
                }
            }
        }

        this.testingConnectionStatus = "testing";

        try {
            const result = await this.dataConnectionService.test(this.connection);
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
        if (!this.isConnectionValid || !this.connection) {
            return;
        }

        try {
            if (this.connection instanceof DatabaseConnection && this.connection.port?.trim() === "") {
                this.connection.port = undefined;
            }

            await this.dataConnectionService.save(this.connection);
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

    @watch<Window>(vm => vm.connection?.name)
    private connectionNameChanged() {
        this.nameField.parentElement?.classList.add("was-validated");

        if (!this.isNameValid()) {
            this.nameField.classList.replace("is-valid", "is-invalid");
            this.nameField.setCustomValidity("Unique name");
        } else {
            this.nameField.classList.replace("is-invalid", "is-valid");
            this.nameField.setCustomValidity("");
        }
    }

    @watch<Window>(vm => vm.authType)
    private async authTypeChanged() {
        if (!this.connection || !(this.connection instanceof DatabaseConnection)) {
            return;
        }

        if (this.authType == "none") {
            this.connection.userId = undefined;
            this.connection.password = undefined;
            this.unprotectedPassword = undefined;
        }
    }

    @watch<Window>(vm => vm.connection?.type)
    @watch<Window>(vm => (vm.connection as DatabaseConnection)?.host)
    @watch<Window>(vm => (vm.connection as DatabaseConnection)?.port)
    @watch<Window>(vm => (vm.connection as DatabaseConnection)?.userId)
    @watch<Window>(vm => (vm.connection as DatabaseConnection)?.password)
    @watch<Window>(vm => (vm.connection as DatabaseConnection)?.databaseName)
    private async updateConnectionString() {
        if (!this.connection) {
            this.connectionString = "";
            return;
        }

        this.connectionString = await this.dataConnectionService.getConnectionString(this.connection);
    }

    private async unprotectedPasswordEntered() {
        const dbConnection = this.connection as DatabaseConnection;
        if (!this.unprotectedPassword) dbConnection.password = this.unprotectedPassword;
        else {
            dbConnection.password = await this.dataConnectionService.protectPassword(this.unprotectedPassword) || undefined;
        }
    }

    private async loadDatabases() {
        if (this.loadingDatabases || !(this.connection instanceof DatabaseConnection)) {
            return;
        }

        const canLoad = !!this.connectionType
            && !!this.connection
            && !!this.connection.host
            && (this.authType !== "userAndPassword" || (!!this.connection.userId && !!this.connection.password));

        if (!canLoad) {
            this.databasesOnServer = undefined;
            return;
        }

        if (!this.databasesOnServer || !this.databasesOnServer.length) {
            this.loadingDatabases = true;

            try {
                this.databasesOnServer = await this.dataConnectionService.getDatabases(this.connection);
            } finally {
                this.loadingDatabases = false;
            }
        }
    }
}

class ConnectionType {
    public label: string;
    public type: DataConnectionType;
}
