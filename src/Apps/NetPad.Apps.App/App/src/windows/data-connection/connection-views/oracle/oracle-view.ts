import {DataConnectionView} from "../data-connection-view";
import {DataConnection, OracleDatabaseConnection} from "@application";
import {CommonServices} from "../common-services";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {HostAndPortComponent} from "../components/host-and-port-component";

export class OracleView extends DataConnectionView<OracleDatabaseConnection> {
    constructor(connection: DataConnection | undefined, commonServices: CommonServices) {
        super(OracleDatabaseConnection, connection);

        this.components = [
            new HostAndPortComponent(this.connection),
            new AuthComponent(this.connection, commonServices),
            new DatabaseComponent(
                this.connection,
                commonServices,
                undefined,
                {
                    enabled: true,
                    requirementsToLoadAreMet: () => this.components.slice(0, 2).every(c => !c.validationError)
                }
            )
        ]
    }
}
