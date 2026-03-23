import {DataConnection, PostgreSqlDatabaseConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {ConnectionDatabaseComponent} from "../components/connection-database-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";

export class PostgresqlView extends DataConnectionView<PostgreSqlDatabaseConnection> {
    constructor(connection: DataConnection | undefined, commonServices: CommonServices) {
        super(PostgreSqlDatabaseConnection, connection);

        this.components = [
            new HostAndPortComponent(this.connection),
            new AuthComponent(this.connection, commonServices),
            new ConnectionDatabaseComponent(
                this.connection,
                commonServices,
                undefined,
                {
                    enabled: true,
                    requirementsToLoadAreMet: () => this.components.slice(0, 2).every(c => !c.validationError),
                })
        ];
    }
}
