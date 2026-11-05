import {DataConnection} from "@application";
import {IDataConnectionViewComponent} from "./components/idata-connection-view-component";

export interface IDataConnectionView {
    readonly connection: DataConnection;
    get validationError(): string | undefined;

    /**
     * A component that grows with the window instead of taking the height of its contents.
     * The window renders it in a slot of its own. There can only be one such component.
     */
    readonly elasticComponent?: IDataConnectionViewComponent;
}
