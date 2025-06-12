import {bindable} from "aurelia";
import {DataConnection} from "@application";

export class DataConnectionName {
    @bindable public connection: DataConnection;

    public get iconUrl(): string {
        if (!this.connection) {
            console.error("Connection not initialized");
            return "";
        }

        switch (this.connection.type) {
            case "MSSQLServer":
                return "/img/mssql.png";
            case "PostgreSQL":
                return "/img/postgresql2.png";
            case "SQLite":
                return "/img/sqlite.png";
            case "MySQL":
                return "/img/mysql.png";
            case "MariaDB":
                return "/img/mariadb.png";
            default:
                return "";
        }
    }
}
