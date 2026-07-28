import {bindable} from "aurelia";
import {observable} from "@aurelia/runtime";
import {EntityFrameworkDatabaseConnection, EntityFrameworkDatabaseServerConnection, ScaffoldOptions} from "@application";

/**
 * The scaffolding options of a connection.
 */
export class ScaffoldingOptions {
    @bindable public connection?: EntityFrameworkDatabaseConnection | EntityFrameworkDatabaseServerConnection;
    @bindable public done: () => void;
    @observable public schemas: string | undefined;
    @observable public tables: string | undefined;

    private scrim: HTMLElement;

    public attached() {
        this.connectionChanged(this.connection);
        this.scrim.focus();
    }

    public get scaffoldOptions(): ScaffoldOptions | undefined {
        if (this.connection && !this.connection.scaffoldOptions) {
            this.connection.scaffoldOptions = new ScaffoldOptions();
        }

        return this.connection?.scaffoldOptions;
    }

    public handleKeyDown(event: KeyboardEvent) {
        if (event.key === "Escape") {
            this.done();
        }
    }

    private connectionChanged(newValue: EntityFrameworkDatabaseConnection | EntityFrameworkDatabaseServerConnection | undefined) {
        this.schemas = newValue?.scaffoldOptions?.schemas?.join("\n") ?? "";
        this.tables = newValue?.scaffoldOptions?.tables?.join("\n") ?? "";
    }

    private schemasChanged(newValue: string | undefined) {
        if (this.scaffoldOptions) {
            this.scaffoldOptions.schemas = ScaffoldingOptions.toLines(newValue);
        }
    }

    private tablesChanged(newValue: string | undefined) {
        if (this.scaffoldOptions) {
            this.scaffoldOptions.tables = ScaffoldingOptions.toLines(newValue);
        }
    }

    private static toLines(value: string | undefined): string[] {
        return value
            ?.split("\n")
            .map(x => x.trim())
            .filter(x => !!x) ?? [];
    }
}
