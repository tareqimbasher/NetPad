import {DatabaseConnection, DatabaseServerConnection, DataConnectionType} from "@application";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";

const defaultPorts = new Map<DataConnectionType, string>([
    ["MSSQLServer", "1433"],
    ["PostgreSQL", "5432"],
    ["MySQL", "3306"],
    ["MariaDB", "3306"],
    ["Oracle", "1521"],
]);

export class HostAndPortComponent implements IDataConnectionViewComponent {
    public readonly touched = {host: false};

    constructor(private readonly connection: DatabaseConnection | DatabaseServerConnection) {
    }

    public get validationError(): string | undefined {
        return !this.connection.host ? "The Host is required." : undefined;
    }

    public get hostError(): string | undefined {
        return this.touched.host && !this.connection.host ? "A host is required." : undefined;
    }

    public get defaultPort(): string | undefined {
        return defaultPorts.get(this.connection.type);
    }
}
