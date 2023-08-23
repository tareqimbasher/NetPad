import {DataConnection} from "@domain";

export interface IDataConnectionView {
    readonly connection: DataConnection;
    get validationError(): string | undefined;
}
