import {DataConnection, IDataConnectionService, MySqlDatabaseConnection, PostgreSqlDatabaseConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {DataConnectionView} from "../data-connection-view";

export class MysqlView extends DataConnectionView<MySqlDatabaseConnection> {
    constructor(connection: DataConnection | undefined, dataConnectionService: IDataConnectionService) {
        super(MySqlDatabaseConnection, connection);

        this.components = [
            new HostAndPortComponent(this.connection),
            new AuthComponent(this.connection, dataConnectionService),
            new DatabaseComponent(
                this.connection,
                undefined,
                {
                    enabled: true,
                    requirementsToLoadAreMet: () => this.components.slice(0, 2).every(c => !c.validationError),
                    dataConnectionService: dataConnectionService
                }
            )
        ]
    }
}
