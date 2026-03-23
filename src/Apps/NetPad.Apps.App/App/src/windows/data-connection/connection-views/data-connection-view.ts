import {Constructable} from "aurelia";
import {
    DatabaseConnection,
    DatabaseServerConnection,
    DataConnection,
    MariaDbDatabaseServerConnection,
    MsSqlServerDatabaseConnection,
    MsSqlServerDatabaseServerConnection,
    MySqlDatabaseConnection,
    MySqlDatabaseServerConnection,
    OracleDatabaseConnection,
    PostgreSqlDatabaseConnection,
    PostgreSqlDatabaseServerConnection,
    SQLiteDatabaseConnection,
    MariaDbDatabaseConnection,
} from "@application";
import {IDataConnectionView} from "./idata-connection-view";
import {IDataConnectionViewComponent} from "./components/idata-connection-view-component";
import {Util} from "@common";

export abstract class DataConnectionView<TDataConnection extends DataConnection> implements IDataConnectionView {
    public readonly connection: TDataConnection;
    protected components: IDataConnectionViewComponent[] = [];

    protected constructor(ctor: Constructable<TDataConnection>, from: DataConnection | undefined) {
        this.connection = this.createNewConnection(ctor, from);
    }

    public get validationError(): string | undefined {
        for (const component of this.components) {
            const error = component.validationError;
            if (error) {
                return error;
            }
        }

        return undefined;
    }

    protected createNewConnection(ctor: Constructable<TDataConnection>, from?: DataConnection): TDataConnection {
        const connection = this.createEmptyConnection(ctor);

        if (from) {
            const newConnectionType = connection.type;
            connection.init(from);
            connection.type = newConnectionType;
        }

        connection.id ||= Util.newGuid();
        connection.name ??= "@localhost";

        if (connection instanceof DatabaseConnection || connection instanceof DatabaseServerConnection) {
            connection.host ||= "localhost";
        }

        return connection;
    }

    private createEmptyConnection(ctor: Constructable<TDataConnection>): TDataConnection {
        const connection = new ctor();

        if (ctor.name === MsSqlServerDatabaseConnection.name || ctor.name === MsSqlServerDatabaseServerConnection.name) {
            connection.type = "MSSQLServer";
        } else if (ctor.name === PostgreSqlDatabaseConnection.name || ctor.name === PostgreSqlDatabaseServerConnection.name) {
            connection.type = "PostgreSQL";
        } else if (ctor.name === SQLiteDatabaseConnection.name) {
            connection.type = "SQLite";
        } else if (ctor.name === MySqlDatabaseConnection.name || ctor.name === MySqlDatabaseServerConnection.name) {
            connection.type = "MySQL";
        } else if (ctor.name === MariaDbDatabaseConnection.name || ctor.name === MariaDbDatabaseServerConnection.name) {
            connection.type = "MariaDB";
        } else if (ctor.name === OracleDatabaseConnection.name) {
            connection.type = "Oracle";
        } else {
            throw new Error("Unhandled data connection type: " + ctor.name);
        }

        return connection;
    }
}
