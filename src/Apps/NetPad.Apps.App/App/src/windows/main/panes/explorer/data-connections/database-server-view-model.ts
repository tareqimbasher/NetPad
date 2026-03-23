import {DatabaseServerConnection} from "@application";
import {DataConnectionViewModel} from "./data-connection-view-model";

export class DatabaseServerViewModel {
    public expanded = false;
    public connections: DataConnectionViewModel[] = [];

    constructor(public server: DatabaseServerConnection) {
    }

    public toggleExpand() {
        this.expanded = !this.expanded;
    }

    public async refresh() {
        await Promise.all(this.connections.map(connVm => connVm.refresh()));
    }
}
