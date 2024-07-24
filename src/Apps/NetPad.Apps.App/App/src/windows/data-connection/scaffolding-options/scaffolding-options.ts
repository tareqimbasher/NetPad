import {bindable} from "aurelia";
import {observable} from "@aurelia/runtime";
import {EntityFrameworkDatabaseConnection, ScaffoldOptions} from "@application";

export class ScaffoldingOptions {
    @bindable public connection?: EntityFrameworkDatabaseConnection;
    @observable public schemas: string | undefined;
    @observable public tables: string | undefined;

    public attached() {
        this.connectionChanged(this.connection);
    }

    private connectionChanged(newValue: EntityFrameworkDatabaseConnection | undefined) {
        this.schemas = newValue?.scaffoldOptions?.schemas?.join("\n") ?? "";
        this.tables = newValue?.scaffoldOptions?.tables?.join("\n") ?? "";
    }

    private schemasChanged(newValue: string | undefined) {
        if (!this.connection) {
            return;
        }

        this.connection.scaffoldOptions ??= new ScaffoldOptions();

        this.connection.scaffoldOptions.schemas = newValue?.split(" ")
            .map(x => x.trim())
            .filter(x => !!x) ?? [];
    }

    private tablesChanged(newValue: string | undefined) {
        if (!this.connection) {
            return;
        }

        this.connection.scaffoldOptions ??= new ScaffoldOptions();

        this.connection.scaffoldOptions.tables = newValue?.split(" ")
            .map(x => x.trim())
            .filter(x => !!x) ?? [];
    }
}
