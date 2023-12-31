import {
    DatabaseConnection,
    DataConnection,
    MsSqlServerDatabaseConnection,
    PostgreSqlDatabaseConnection,
    SQLiteDatabaseConnection
} from "@domain";
import {IDataConnectionView} from "./idata-connection-view";
import {IDataConnectionViewComponent} from "./components/idata-connection-view-component";
import {Util} from "@common";
import {Constructable} from "aurelia";

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
            connection.id = from.id;
            connection.name = from.name;

            if (connection instanceof DatabaseConnection && from instanceof DatabaseConnection) {
                // TODO bad solution. find better way to selectively copy this data without listing each prop
                connection.host = from.host;
                connection.databaseName = from.databaseName;
                connection.userId = from.userId;
                connection.password = from.password;
                connection.containsProductionData = from.containsProductionData;
                connection.connectionStringAugment = from.connectionStringAugment;
            }
        }

        connection.id ||= Util.newGuid();
        connection.name ??= "@localhost";

        if (connection instanceof DatabaseConnection) {
            connection.host ||= "localhost";
        }

        return connection;
    }

    private createEmptyConnection(ctor: Constructable<TDataConnection>): TDataConnection {
        const connection = new ctor();

        if (ctor.name === MsSqlServerDatabaseConnection.name) {
            connection.type = "MSSQLServer";
        } else if (ctor.name === PostgreSqlDatabaseConnection.name) {
            connection.type = "PostgreSQL";
        } else if (ctor.name === SQLiteDatabaseConnection.name) {
            connection.type = "SQLite";
        } else {
            throw new Error("Unhandled data connection type: " + ctor.name);
        }

        return connection;
    }
}
