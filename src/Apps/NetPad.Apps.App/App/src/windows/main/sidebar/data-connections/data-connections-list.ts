import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {ApiException, DatabaseStructure, DataConnection, DataConnectionStore, IDataConnectionService} from "@domain";
import {ContextMenuOptions, ViewModelBase} from "@application";

export class DataConnectionsList extends ViewModelBase {
    public dataConnections: SidebarDataConnection[] = [];
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        private readonly dataConnectionStore: DataConnectionStore,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public binding() {
        this.tabContextMenuOptions = new ContextMenuOptions(".list-group-item.data-connection", [
            {
                icon: "delete-icon",
                text: "Delete",
                onSelected: async (clickTarget) => this.delete(this.getDataConnectionId(clickTarget))
            }
        ]);
    }

    public async attached() {
        this.constructSideBarDataConnections();
    }

    public async addConnection() {
        await this.dataConnectionService.openDataConnectionWindow(null);
    }

    public async delete(connectionId: string) {
        const connection = this.dataConnections.find(c => c.id === connectionId);
        if (!connection) return;

        if (confirm(`Are you sure you want to delete "${connection.name}"?`)) {
            await this.dataConnectionService.delete(connectionId);
        }
    }

    @watch<DataConnectionsList>(vm => vm.dataConnectionStore.connections)
    private constructSideBarDataConnections() {
        this.dataConnections = this.dataConnectionStore.connections
            .map(c => new SidebarDataConnection(c, this.dataConnectionService));
    }

    private getDataConnectionId(element: Element) {
        return element.getAttribute("data-connection-id");
    }
}

class SidebarDataConnection extends DataConnection {
    public expanded = false;
    public structure?: DatabaseStructure;
    public error?: string;
    public loadingStructure = false;

    constructor(connection: DataConnection, private readonly dataConnectionService: IDataConnectionService) {
        super(connection);
    }

    public toggleExpand() {
        this.expanded = !this.expanded;

        if (this.expanded && !this.structure && !this.loadingStructure) {
            this.loadingStructure = true;
            this.error = null;

            this.dataConnectionService.getDatabaseStructure(this.id)
                .then(structure => {
                    this.structure = structure;
                })
                .catch((err: ApiException) => {
                    if (err.response) {
                        const serverResponse = JSON.parse(err.response);
                        if (serverResponse?.message) {
                            this.error = serverResponse.message;
                        }
                    }

                    if (!this.error) {
                        this.error = err.message;
                    }
                })
                .finally(() => this.loadingStructure = false);
        }
    }
}
