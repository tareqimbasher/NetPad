import {
    DataConnection,
    DataConnectionDeletedEvent,
    DataConnectionSavedEvent,
    IDataConnectionService,
    IEventBus
} from "@application";
import {ILogger} from "aurelia";

export class DataConnectionStore {
    private readonly logger: ILogger;

    public connections: DataConnection[] = [];

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(DataConnectionStore));
        this.subscribeToServerEvents();
    }

    public async initialize() {
        try {
            this.connections = await this.dataConnectionService.getAll();
        } catch (ex) {
            this.logger.error("Error loading data connections", ex);
        }
    }

    private subscribeToServerEvents() {
        this.eventBus.subscribeToServer(DataConnectionSavedEvent, msg => {
            const ix = this.connections.findIndex(c => c.id === msg.dataConnection.id);
            const connection = DataConnection.fromJS(msg.dataConnection);

            if (ix >= 0) {
                this.connections[ix].init(connection);
            } else {
                this.connections.push(connection);
            }
        });

        this.eventBus.subscribeToServer(DataConnectionDeletedEvent, msg => {
            const ix = this.connections.findIndex(c => c.id === msg.dataConnection.id);
            if (ix >= 0)
                this.connections.splice(ix, 1);
        });
    }
}
