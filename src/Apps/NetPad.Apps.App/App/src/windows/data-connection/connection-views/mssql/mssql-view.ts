import {DataConnection, MsSqlServerDatabaseConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {ConnectionDatabaseComponent} from "../components/connection-database-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";
import {hasTrustServerCertificate, setConnectionStringKey} from "../connection-string-util";

export class MssqlView extends DataConnectionView<MsSqlServerDatabaseConnection> {
    constructor(connection: DataConnection | undefined, commonServices: CommonServices) {
        super(MsSqlServerDatabaseConnection, connection);

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

    public get trustServerCertificate(): boolean {
        return hasTrustServerCertificate(this.connection);
    }

    public set trustServerCertificate(value: boolean) {
        setConnectionStringKey(this.connection, "Trust Server Certificate", value ? "True" : null);
        setConnectionStringKey(this.connection, "TrustServerCertificate", null);
    }
}
