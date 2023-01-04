import {
    DatabaseConnection,
    DataConnection,
    DataConnectionType,
    IDataConnectionService,
    MsSqlServerDatabaseConnection,
    PostgreSqlDatabaseConnection,
    Settings
} from "@domain";
import {Util} from "@common";
import {Constructable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";

export class Window {
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
    private nameField: HTMLInputElement;
    private unprotectedPassword?: string;
    private readonly logger: ILogger;

    constructor(
        private readonly startupOptions: URLSearchParams,
        private readonly settings: Settings,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @ILogger logger: ILogger
    ) {
        this.logger = logger.scopeTo(nameof(Window));

        const createNew = !this.startupOptions.get("data-connection-id");
        document.title = createNew ? "New Connection" : "Edit Connection";
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
            alert("Could not save the connection: " + ex.toString());
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
