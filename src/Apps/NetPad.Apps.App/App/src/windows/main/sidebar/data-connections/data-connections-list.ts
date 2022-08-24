import {
    ApiException,
    DatabaseStructure,
    DataConnection,
    DataConnectionDeletedEvent,
    DataConnectionSavedEvent,
    IDataConnectionService,
    IEventBus
} from "@domain";
import {ILogger} from "aurelia";
import {ContextMenuOptions, ViewModelBase} from "@application";

export class DataConnectionsList extends ViewModelBase {
    public dataConnections: SidebarDataConnection[] = [];
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IEventBus private readonly eventBus: IEventBus,
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
        try {
            this.dataConnections = (await this.dataConnectionService.getAll())
                .map(c => new SidebarDataConnection(c, this.dataConnectionService));
        } catch (ex) {
            this.logger.error("Error loading data connections", ex);
        }

        this.eventBus.subscribeToServer(DataConnectionSavedEvent, msg => {
            const ix = this.dataConnections.findIndex(c => c.id === msg.dataConnection.id);
            const connection = DataConnection.fromJS(msg.dataConnection);

            if (ix >= 0) {
                this.dataConnections[ix].init(connection);
            } else {
                this.dataConnections.push(new SidebarDataConnection(connection, this.dataConnectionService));
            }
        });

        this.eventBus.subscribeToServer(DataConnectionDeletedEvent, msg => {
            const ix = this.dataConnections.findIndex(c => c.id === msg.dataConnection.id);
            if (ix >= 0)
                this.dataConnections.splice(ix, 1);
        });
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
