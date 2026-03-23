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

        this.eventBus.subscribeToServer(DatabaseServerSavedEvent, msg => {
            const ix = this.servers.findIndex(s => s.id === msg.server.id);
            const server = DatabaseServerConnection.fromJS(msg.server);

            if (ix >= 0) {
                this.servers[ix].init(server);
            } else {
                this.servers.push(server);
            }
        });

        this.eventBus.subscribeToServer(DatabaseServerDeletedEvent, msg => {
            const ix = this.servers.findIndex(s => s.id === msg.server.id);
            if (ix >= 0)
                this.servers.splice(ix, 1);
        });
    }
}
