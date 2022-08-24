import {
    DatabaseConnection,
    DataConnectionType,
    IDataConnectionService,
    PostgreSqlDatabaseConnection,
    Settings,
    MsSqlServerDatabaseConnection
} from "@domain";
import {Util} from "@common";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";

export class Window {
    public connection?: DatabaseConnection;
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
    public databasesOnServer?: string[];
    public prohibitedNames: string[] = [];
    private nameField: HTMLInputElement;
    private readonly logger: ILogger;

    constructor(
        private readonly startupOptions: URLSearchParams,
        private readonly settings: Settings,
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @ILogger logger: ILogger
    ) {
        this.logger = logger.scopeTo(nameof(Window));
        const dataConnectionId = this.startupOptions.get("data-connection-id");
        const createNew = !dataConnectionId;

        document.title = createNew ? "New Connection" : "Edit Connection";
    }

    public async binding() {
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
        return !!this.connectionType
            && !!this.connection
            && this.isNameValid
            && !!this.connection.host
            && !!this.connection.databaseName
            && (this.authType !== "userAndPassword" || (!!this.connection.userId && !!this.connection.password));
    }

    public get isNameValid() {
        if (!this.connection?.name) {
            return true;
        }

        return this.prohibitedNames.indexOf(this.connection.name) < 0;
    }

    public setConnectionType(connectionType: ConnectionType) {
        if (this.testingConnectionStatus === "testing") {
            return;
        }

        this.connectionType = connectionType;

        if (connectionType.type === "MSSQLServer") {
            this.connection = new MsSqlServerDatabaseConnection({
                id: Util.newGuid(),
                type: "MSSQLServer",
                name: "New Microsoft SQL Server Connection",
                entityFrameworkProviderName: "",
                containsProductionData: false,

                // Test
                host: "localhost",
                databaseName: "NetPad",
            });
        } else if (connectionType.type === "PostgreSQL") {
            // test
            this.authType = "userAndPassword";

            this.connection = new PostgreSqlDatabaseConnection({
                id: Util.newGuid(),
                type: "PostgreSQL",
                name: "New PostgreSQL Connection",
                entityFrameworkProviderName: "",
                containsProductionData: false,
                port: "5432",

                // Test
                host: "localhost",
                databaseName: "netpad",
                userId: "postgres",
                password: "password"
            });
        } else {
            this.connection = null;
        }
    }

    public async testConnection() {
        if (!this.connectionType) {
            alert("Configure the connection first.");
            return;
        }

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
        if (!this.isConnectionValid) {
            return;
        }

        try {
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
        this.nameField.parentElement.classList.add("was-validated");

        if (!this.isNameValid) {
            this.nameField.classList.replace("is-valid", "is-invalid");
            this.nameField.setCustomValidity("Unique name");
        }
        else {
            this.nameField.classList.replace("is-invalid", "is-valid");
            this.nameField.setCustomValidity("");
        }
    }

    private async loadDatabases() {
        const canLoad = !!this.connectionType
            && !!this.connection
            && !!this.connection.host
            && (this.authType !== "userAndPassword" || (!!this.connection.userId && !!this.connection.password));

        if (!canLoad) {
            this.databasesOnServer = null;
            return;
        }

        if (!this.databasesOnServer || !this.databasesOnServer.length)
            this.databasesOnServer = await this.dataConnectionService.getDatabases(this.connection);
    }
}

class ConnectionType {
    public label: string;
    public type: DataConnectionType;
}
