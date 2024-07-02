import {DatabaseConnection} from "@application";
import {IDataConnectionViewComponent} from "./idata-connection-view-component";

export class HostAndPortComponent implements IDataConnectionViewComponent {
    constructor(private readonly connection: DatabaseConnection) {
    }

    public get validationError(): string | undefined {
        return !this.connection.host ? "The Host is required." : undefined;
    }
}
