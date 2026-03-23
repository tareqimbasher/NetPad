import {Constructable} from "aurelia";
import {DatabaseServerConnection, DataConnection, MsSqlServerDatabaseServerConnection} from "@application";
import {HostAndPortComponent} from "../components/host-and-port-component";
import {AuthComponent} from "../components/auth-component";
import {DatabaseSelectionComponent} from "../components/database-selection-component";
import {DataConnectionView} from "../data-connection-view";
import {CommonServices} from "../common-services";

export class ServerView extends DataConnectionView<DatabaseServerConnection> {
    public databaseSelectionComponent: DatabaseSelectionComponent;

    constructor(ctor: Constructable<DatabaseServerConnection>, connection: DataConnection | undefined, commonServices: CommonServices) {
        super(ctor, connection);

        const hostAndPort = new HostAndPortComponent(this.connection);
        const auth = new AuthComponent(this.connection, commonServices);
        this.databaseSelectionComponent = new DatabaseSelectionComponent(
            this.connection,
            commonServices,
            {
                requirementsToLoadAreMet: () => [hostAndPort, auth].every(c => !c.validationError),
            });

        this.components = [
            hostAndPort,
            auth,
            this.databaseSelectionComponent,
        ];
    }

    public get isMssql(): boolean {
        return this.connection instanceof MsSqlServerDatabaseServerConnection;
    }

    public get trustServerCertificate(): boolean {
        const augment = this.connection.connectionStringAugment?.toLowerCase().replaceAll(" ", "");
        return !!augment && augment.indexOf("TrustServerCertificate=True".toLowerCase()) >= 0;
    }

    public set trustServerCertificate(value: boolean) {
        this.setConnectionStringKey("Trust Server Certificate", value ? "True" : null);
        this.setConnectionStringKey("TrustServerCertificate", null);
    }

    public setConnectionStringKey(key: string, value: string | null) {
        if (!this.connection.connectionStringAugment) {
            this.connection.connectionStringAugment = `${key}=${value};`;
            return;
        }

        const kvs = this.connection.connectionStringAugment
            .split(";")
            .map(i => i.trim())
            .filter(i => !!i)
            .map(s => s.split("="))
            .filter(s => s.length >= 2);

        let found = false;

        const keyLowered = key.toLowerCase();
        for (const kv of kvs) {
            if (kv[0].toLowerCase() !== keyLowered) {
                continue;
            }

            found = true;

            if (value === null) {
                kv.splice(0);
            } else {
                kv.splice(1);
                kv.push(value);
            }
        }

        if (!found && value !== null) {
            kvs.push([key, value]);
        }

        this.connection.connectionStringAugment = kvs
            .filter(kv => kv.length > 0)
            .map(kv => `${kv[0]}=${kv.slice(1).join("=")}`)
            .join(";") + ";";
    }
}
