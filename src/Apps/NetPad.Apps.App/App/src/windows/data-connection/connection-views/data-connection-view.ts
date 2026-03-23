import {Constructable} from "aurelia";
import {
    DatabaseConnection,
    DatabaseServerConnection,
    DataConnection,
    DataConnectionType,
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

const connectionTypeMap = new Map<Constructable, DataConnectionType>([
    [MsSqlServerDatabaseConnection, "MSSQLServer"],
    [MsSqlServerDatabaseServerConnection, "MSSQLServer"],
    [PostgreSqlDatabaseConnection, "PostgreSQL"],
    [PostgreSqlDatabaseServerConnection, "PostgreSQL"],
    [SQLiteDatabaseConnection, "SQLite"],
    [MySqlDatabaseConnection, "MySQL"],
    [MySqlDatabaseServerConnection, "MySQL"],
    [MariaDbDatabaseConnection, "MariaDB"],
    [MariaDbDatabaseServerConnection, "MariaDB"],
    [OracleDatabaseConnection, "Oracle"],
]);

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

        const type = connectionTypeMap.get(ctor);
        if (!type) {
            throw new Error("Unhandled data connection type: " + ctor.name);
        }
        connection.type = type;

        return connection;
    }
}
