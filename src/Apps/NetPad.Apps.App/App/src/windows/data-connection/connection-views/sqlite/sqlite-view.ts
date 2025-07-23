import {Constructable} from "aurelia";
import {DataConnection, MsSqlServerDatabaseConnection, SQLiteDatabaseConnection} from "@application";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {DataConnectionView} from "../data-connection-view";
import {WindowParams} from "@application/windows/window-params";
import {ShellType} from "@application/windows/shell-type";
import {CommonServices} from "../common-services";

export class SqliteView extends DataConnectionView<SQLiteDatabaseConnection> {
    constructor(connection: DataConnection | undefined, commonServices: CommonServices) {
        super(SQLiteDatabaseConnection, connection);

        this.components = [
            new AuthComponent(this.connection, commonServices, true),
            new DatabaseComponent(this.connection, commonServices,{
                // Until we implement a way to select a SQLite file from user's machine from the browser and be able
                // to get its full path, this option will not be available to browser shell.
                allowSelectDatabaseFile: WindowParams.shell !== ShellType.Browser
            })
        ];
    }

    protected override createNewConnection(ctor: Constructable<MsSqlServerDatabaseConnection>, from?: DataConnection): SQLiteDatabaseConnection {
        const connection = super.createNewConnection(ctor, from);

        connection.host = undefined;

        return connection;
    }
}
