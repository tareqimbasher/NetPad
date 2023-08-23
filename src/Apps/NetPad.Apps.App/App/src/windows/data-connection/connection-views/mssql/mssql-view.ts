import {DataConnection, IDataConnectionService, MsSqlServerDatabaseConnection} from "@domain";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {DatabaseComponent} from "../components/database-component";
import {DataConnectionView} from "../data-connection-view";

export class MssqlView extends DataConnectionView<MsSqlServerDatabaseConnection> {
    constructor(connection: DataConnection | undefined, dataConnectionService: IDataConnectionService) {
        super(MsSqlServerDatabaseConnection, connection);

        this.components = [
            new HostAndPortComponent(this.connection),
            new AuthComponent(this.connection, dataConnectionService),
            new DatabaseComponent(this.connection, {
                enabled: true,
                requirementsToLoadAreMet: () => this.components.slice(0, 2).every(c => !c.validationError),
                dataConnectionService: dataConnectionService
            })
        ];
    }
}
