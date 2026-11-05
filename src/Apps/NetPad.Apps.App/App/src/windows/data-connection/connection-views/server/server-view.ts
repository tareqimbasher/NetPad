import {Constructable} from "aurelia";
import {DatabaseServerConnection, DataConnection, MsSqlServerDatabaseServerConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {ServerDatabasesComponent} from "../components/server-databases-component";
import {IDataConnectionViewComponent} from "../components/idata-connection-view-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";
import {hasTrustServerCertificate, setConnectionStringKey} from "../connection-string-util";

export class ServerView extends DataConnectionView<DatabaseServerConnection> {
    public readonly credentialComponents: IDataConnectionViewComponent[];

    /** The database list grows with the window, the window renders it in its elastic slot. */
    public readonly elasticComponent: ServerDatabasesComponent;

    constructor(ctor: Constructable<DatabaseServerConnection>, connection: DataConnection | undefined, commonServices: CommonServices) {
        super(ctor, connection);

        const hostAndPort = new HostAndPortComponent(this.connection);
        const auth = new AuthComponent(this.connection, commonServices);
        this.credentialComponents = [hostAndPort, auth];
        this.elasticComponent = new ServerDatabasesComponent(
            this.connection,
            commonServices,
            {
                requirementsToLoadAreMet: () => this.credentialComponents.every(c => !c.validationError),
            });

        this.components = [
            ...this.credentialComponents,
            this.elasticComponent,
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
