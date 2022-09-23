import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    ApiException,
    DatabaseStructure,
    DataConnection,
    DataConnectionResourceComponent,
    DataConnectionResourcesUpdatedEvent,
    DataConnectionResourcesUpdateFailedEvent,
    DataConnectionResourcesUpdatingEvent,
    DataConnectionStore,
    IDataConnectionService,
    IEventBus
} from "@domain";
import {ContextMenuOptions, ViewModelBase} from "@application";

export class DataConnectionsList extends ViewModelBase {
    public dataConnectionViewModels: DataConnectionViewModel[] = [];
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        private readonly dataConnectionStore: DataConnectionStore,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public binding() {
        this.tabContextMenuOptions = new ContextMenuOptions(".list-group-item.data-connection", [
            {
                icon: "refresh-icon",
                text: "Refresh",
                onSelected: async (clickTarget) => this.refresh(this.getDataConnectionId(clickTarget))
            },
            {
                icon: "delete-icon",
                text: "Delete",
                onSelected: async (clickTarget) => this.delete(this.getDataConnectionId(clickTarget))
            },
            {
                isDivider: true
            },
            {
                icon: "configure-icon",
                text: "Properties",
                onSelected: async (clickTarget) => this.editConnection(this.getDataConnectionId(clickTarget))
            }
        ]);

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdatingEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceBeingLoaded(msg.updatingComponent);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdatedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceCompletedLoading(msg.updatedComponent);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionResourcesUpdateFailedEvent, msg => {
            const vm = this.dataConnectionViewModels.find(v => v.connection.id == msg.dataConnection.id);
            if (vm) {
                vm.resourceFailedLoading(msg.failedComponent, msg.error);
            }
        });
    }

    public async attached() {
        this.constructSideBarDataConnections();
    }

    public async addConnection() {
        await this.dataConnectionService.openDataConnectionWindow(null);
    }

    public async editConnection(connectionId: string) {
        await this.dataConnectionService.openDataConnectionWindow(connectionId);
    }

    public async delete(connectionId: string) {
        const connection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (!connection) return;

        if (confirm(`Are you sure you want to delete "${connection.connection.name}"?`)) {
            await this.dataConnectionService.delete(connectionId);
        }
    }

    public async refresh(connectionId: string) {
        const dataConnection = this.dataConnectionViewModels.find(v => v.connection.id === connectionId);
        if (dataConnection) {
            await dataConnection.refresh();
        }
    }

    @watch<DataConnectionsList>(vm => vm.dataConnectionStore.connections)
    private constructSideBarDataConnections() {
        this.dataConnectionViewModels = this.dataConnectionStore.connections
            .map(c => new DataConnectionViewModel(c, this.dataConnectionService));
    }

    private getDataConnectionId(element: Element) {
        return element.getAttribute("data-connection-id");
    }
}

class DataConnectionViewModel {
    public expanded = false;
    public structure?: DatabaseStructure;
    public error?: string;
    public loadingStructure = false
    public resourceLoading = new Set<DataConnectionResourceComponent>();

    constructor(public connection: DataConnection, private readonly dataConnectionService: IDataConnectionService) {
    }

    public get loadingMessage(): string | null {
        if (this.resourceLoading.size > 0) {
            const onlyLoadingAssembly = this.resourceLoading.size == 1 && Array.from(this.resourceLoading)[0] === "Assembly";
            return onlyLoadingAssembly ? "Loading" : "Scaffolding";
        }

        return this.loadingStructure ? "Loading" : null
    }

    public toggleExpand() {
        this.expanded = !this.expanded;

        if (this.expanded && !this.structure) {
            this.getDatabaseStructure();
        }
    }

    public async refresh() {
        await this.dataConnectionService.refresh(this.connection.id);
        this.getDatabaseStructure();
    }

    public getDatabaseStructure() {
        if (this.loadingStructure) return;

        this.loadingStructure = true;
        this.error = null;

        this.dataConnectionService.getDatabaseStructure(this.connection.id)
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

    public resourceBeingLoaded(component: DataConnectionResourceComponent) {
        this.resourceLoading.add(component);
    }

    public resourceCompletedLoading(component: DataConnectionResourceComponent) {
        this.resourceLoading.delete(component);
        this.error = null;
    }

    public resourceFailedLoading(component: DataConnectionResourceComponent, error: string | null) {
        this.resourceLoading.delete(component);
        this.error = error || "Error";
    }
}
