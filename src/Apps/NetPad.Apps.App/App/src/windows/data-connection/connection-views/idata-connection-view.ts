import {DataConnection} from "@application";

export interface IDataConnectionView {
    readonly connection: DataConnection;
    get validationError(): string | undefined;
}
