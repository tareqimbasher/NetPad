import {Constructable} from "aurelia";
import {
    DataConnection,
    IDataConnectionService,
    MsSqlServerDatabaseConnection,
    SQLiteDatabaseConnection
} from "@application";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {DataConnectionView} from "../data-connection-view";

export class SqliteView extends DataConnectionView<SQLiteDatabaseConnection> {
    constructor(connection: DataConnection | undefined, dataConnectionService: IDataConnectionService) {
        super(SQLiteDatabaseConnection, connection);

        this.components = [
            new AuthComponent(this.connection, dataConnectionService, true),
            new DatabaseComponent(this.connection, {
                allowSelectDatabaseFile: true
            })
        ];
    }

    protected override createNewConnection(ctor: Constructable<MsSqlServerDatabaseConnection>, from?: DataConnection): SQLiteDatabaseConnection {
        const connection = super.createNewConnection(ctor, from);

        connection.host = undefined;

        return connection;
    }
}
