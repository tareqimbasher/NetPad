import {Constructable} from "aurelia";
import {DatabaseServerConnection, DataConnection, MsSqlServerDatabaseServerConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {ServerDatabasesComponent} from "../components/server-databases-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";
import {hasTrustServerCertificate, setConnectionStringKey} from "../connection-string-util";

export class ServerView extends DataConnectionView<DatabaseServerConnection> {
    public serverDatabasesComponent: ServerDatabasesComponent;

    constructor(ctor: Constructable<DatabaseServerConnection>, connection: DataConnection | undefined, commonServices: CommonServices) {
        super(ctor, connection);

        const hostAndPort = new HostAndPortComponent(this.connection);
        const auth = new AuthComponent(this.connection, commonServices);
        this.serverDatabasesComponent = new ServerDatabasesComponent(
            this.connection,
            commonServices,
            {
                requirementsToLoadAreMet: () => [hostAndPort, auth].every(c => !c.validationError),
            });

        this.components = [
            hostAndPort,
            auth,
            this.serverDatabasesComponent,
        ];
    }

    public get isMssql(): boolean {
        return this.connection instanceof MsSqlServerDatabaseServerConnection;
    }

    public get trustServerCertificate(): boolean {
        return hasTrustServerCertificate(this.connection);
    }

    public set trustServerCertificate(value: boolean) {
        setConnectionStringKey(this.connection, "Trust Server Certificate", value ? "True" : null);
        setConnectionStringKey(this.connection, "TrustServerCertificate", null);
    }
}
