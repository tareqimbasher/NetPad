import {DataConnection, MsSqlServerDatabaseConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {ConnectionDatabaseComponent} from "../components/connection-database-component";
import {IDataConnectionViewComponent} from "../components/idata-connection-view-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";
import {hasTrustServerCertificate, setConnectionStringKey} from "../connection-string-util";

export class MssqlView extends DataConnectionView<MsSqlServerDatabaseConnection> {
    public readonly credentialComponents: IDataConnectionViewComponent[];
    public readonly databaseComponent: ConnectionDatabaseComponent;

    constructor(connection: DataConnection | undefined, commonServices: CommonServices) {
        super(MsSqlServerDatabaseConnection, connection);

        this.credentialComponents = [
            new HostAndPortComponent(this.connection),
            new AuthComponent(this.connection, commonServices),
        ];

        this.databaseComponent = new ConnectionDatabaseComponent(
            this.connection,
            commonServices,
            undefined,
            {
                enabled: true,
                requirementsToLoadAreMet: () => this.credentialComponents.every(c => !c.validationError),
            });

        this.components = [...this.credentialComponents, this.databaseComponent];
    }

    public get trustServerCertificate(): boolean {
        return hasTrustServerCertificate(this.connection);
    }

    public set trustServerCertificate(value: boolean) {
        setConnectionStringKey(this.connection, "Trust Server Certificate", value ? "True" : null);
        setConnectionStringKey(this.connection, "TrustServerCertificate", null);
    }
}
