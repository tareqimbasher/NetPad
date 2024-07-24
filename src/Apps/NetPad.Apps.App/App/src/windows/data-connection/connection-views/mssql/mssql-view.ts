import {DataConnection, IDataConnectionService, MsSqlServerDatabaseConnection} from "@application";
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
