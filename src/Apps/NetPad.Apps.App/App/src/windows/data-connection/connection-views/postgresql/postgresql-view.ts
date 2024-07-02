import {DataConnection, IDataConnectionService, PostgreSqlDatabaseConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {DataConnectionView} from "../data-connection-view";

export class PostgresqlView extends DataConnectionView<PostgreSqlDatabaseConnection> {
    constructor(connection: DataConnection | undefined, dataConnectionService: IDataConnectionService) {
        super(PostgreSqlDatabaseConnection, connection);

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
                })
        ];
    }
}
