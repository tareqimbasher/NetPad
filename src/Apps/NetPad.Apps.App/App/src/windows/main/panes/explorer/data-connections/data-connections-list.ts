import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    DataConnectionResourcesUpdatedEvent,
    DataConnectionResourcesUpdateFailedEvent,
    DataConnectionResourcesUpdatingEvent,
    DataConnectionStore,
    IDataConnectionService,
    IEventBus
} from "@domain";
import {ContextMenuOptions, ViewModelBase} from "@application";
import {DataConnectionViewModel} from "./data-connection-view-model";

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
        this.constructDataConnectionViewModels();
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
    private constructDataConnectionViewModels() {
        this.dataConnectionViewModels = this.dataConnectionStore.connections
            .map(c => new DataConnectionViewModel(c, this.dataConnectionService));
    }

    private getDataConnectionId(element: Element) {
        return element.getAttribute("data-connection-id");
    }
}
