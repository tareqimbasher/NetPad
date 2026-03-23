import {
    DatabaseServerConnection,
    DatabaseServerDeletedEvent,
    DatabaseServerSavedEvent,
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
    public servers: DatabaseServerConnection[] = [];

    constructor(
        @IDataConnectionService private readonly dataConnectionService: IDataConnectionService,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(DataConnectionStore));
        this.subscribeToServerEvents();
    }

    public async initialize() {
        try {
            const response = await this.dataConnectionService.getAll();
            this.connections = response.connections;
            this.servers = response.servers;
        } catch (ex) {
            this.logger.error("Error loading data connections", ex);
        }
    }

    private subscribeToServerEvents() {
        this.eventBus.subscribeToServer(DataConnectionSavedEvent, msg => {
            const ix = this.connections.findIndex(c => c.id === msg.dataConnection.id);

            if (ix >= 0) {
                this.connections[ix].init(msg.dataConnection);
            } else {
                this.connections.push(DataConnection.fromJS(msg.dataConnection));
            }
        });

        this.eventBus.subscribeToServer(DataConnectionDeletedEvent, msg => {
            const ix = this.connections.findIndex(c => c.id === msg.dataConnection.id);
            if (ix >= 0)
                this.connections.splice(ix, 1);
        });

        this.eventBus.subscribeToServer(DatabaseServerSavedEvent, msg => {
            const ix = this.servers.findIndex(s => s.id === msg.server.id);

            if (ix >= 0) {
                this.servers[ix].init(msg.server);
            } else {
                this.servers.push(DatabaseServerConnection.fromJS(msg.server));
            }
        });

        this.eventBus.subscribeToServer(DatabaseServerDeletedEvent, msg => {
            const ix = this.servers.findIndex(s => s.id === msg.server.id);
            if (ix >= 0)
                this.servers.splice(ix, 1);
        });
    }
}
