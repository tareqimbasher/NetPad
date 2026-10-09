import {observable} from "@aurelia/runtime";
import {EntityFrameworkDatabaseConnection, EntityFrameworkDatabaseServerConnection, ScaffoldOptions} from "@application";
import {Dialog} from "@application/dialogs/dialog";

export type ScaffoldableConnection = EntityFrameworkDatabaseConnection | EntityFrameworkDatabaseServerConnection;

/**
 * The scaffolding options of a connection. Edits apply to the connection as they are made; the
 * connection is saved, or not, with the rest of the window.
 */
export class ScaffoldingOptionsDialog extends Dialog<ScaffoldableConnection> {
    @observable public schemas: string | undefined;
    @observable public tables: string | undefined;

    public get scaffoldOptions(): ScaffoldOptions | undefined {
        if (this.input && !this.input.scaffoldOptions) {
            this.input.scaffoldOptions = new ScaffoldOptions();
        }

        return this.input?.scaffoldOptions;
    }

    public bound() {
        this.schemas = this.input?.scaffoldOptions?.schemas?.join("\n") ?? "";
        this.tables = this.input?.scaffoldOptions?.tables?.join("\n") ?? "";
    }

    private schemasChanged(newValue: string | undefined) {
        if (this.scaffoldOptions) {
            this.scaffoldOptions.schemas = ScaffoldingOptionsDialog.toLines(newValue);
        }
    }

    private tablesChanged(newValue: string | undefined) {
        if (this.scaffoldOptions) {
            this.scaffoldOptions.tables = ScaffoldingOptionsDialog.toLines(newValue);
        }
    }

    private static toLines(value: string | undefined): string[] {
        return value
            ?.split("\n")
            .map(x => x.trim())
            .filter(x => !!x) ?? [];
    }
}
