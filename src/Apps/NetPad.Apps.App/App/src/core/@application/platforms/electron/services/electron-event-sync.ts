import {Constructable, ILogger} from "aurelia";
import {WithDisposables} from "@common";
import {AppActivatedEvent, ChannelInfo, IBackgroundService, IEventBus} from "@application";
import {ElectronIpcGateway} from "./electron-ipc-gateway";

/**
 * Forwards events between the main Electron process and the app's EventBus.
 */
export class ElectronEventSync extends WithDisposables implements IBackgroundService {
    /**
     * IPC events from Electron's main process that should be forwarded to application's EventBus.
     */
    private mainToEventBus: Constructable[] = []; // Empty, no events need to be forwarded currently.

    /**
     * Events from application EventBus that should be forwarded to the Electron main process.
     */
    private eventBusToMain: Constructable[] = [AppActivatedEvent];

    constructor(private readonly electronIpcGateway: ElectronIpcGateway,
                @IEventBus private readonly eventBus: IEventBus,
                @ILogger private readonly logger: ILogger) {
        super();
        this.logger = logger.scopeTo(nameof(ElectronEventSync));
    }

    public start(): Promise<void> {
        for (const type of this.mainToEventBus) {
            this.forwardIpcMainChannelToEventBus(type)
        }

        for (const type of this.eventBusToMain) {
            this.forwardEventBusChannelToIpcMain(type)
        }

        return Promise.resolve();
    }

    public stop(): void {
        this.dispose();
    }

    /**
     * Listens to Electron Main process messages from a specific channel and forwards them to the app's EventBus.
     * @param typeOrChannelName The name, or message type, of the Electron main process channel to listen on.
     */
    private forwardIpcMainChannelToEventBus(typeOrChannelName: Constructable | string): void {
        const channel = new ChannelInfo(typeOrChannelName);

        this.addDisposable(
            this.electronIpcGateway.subscribe(channel, (message, channelInfo) => {
                this.eventBus.publish(message as object);
            })
        );
    }

    /**
     * Listens to app EventBus messages from a specific channel and forwards them to the Electron Main process.
     * @param typeOrChannelName The name, or message type, of the EventBus channel to listen on.
     */
    private forwardEventBusChannelToIpcMain(typeOrChannelName: Constructable | string): void {
        const channel = new ChannelInfo(typeOrChannelName);

        const handler = async (message: unknown) => {
            await this.electronIpcGateway.send(channel, message);
        };

        this.addDisposable(
            channel.messageType
                ? this.eventBus.subscribe(channel.messageType, handler)
                : this.eventBus.subscribe(channel.name, handler)
        );
    }
}
