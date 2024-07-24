import {Constructable} from "aurelia";
import {
    DatabaseConnection,
    DataConnection,
    MsSqlServerDatabaseConnection,
    PostgreSqlDatabaseConnection,
    SQLiteDatabaseConnection
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
