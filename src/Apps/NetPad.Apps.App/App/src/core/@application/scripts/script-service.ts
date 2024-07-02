import {DI} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {
    IEventBus,
    IScriptsApiClient,
    ScriptCodeUpdatedEvent,
    ScriptCodeUpdatingEvent,
    ScriptsApiClient
} from "@application";

export interface IScriptService extends IScriptsApiClient {}

export const IScriptService = DI.createInterface<IScriptService>();

export class ScriptService extends ScriptsApiClient implements IScriptService {

    constructor(@IEventBus private readonly eventBus: IEventBus, baseUrl?: string, @IHttpClient http?: IHttpClient) {
        super(baseUrl, http);
    }

    public override async updateCode(id: string, code: string, signal?: AbortSignal | undefined): Promise<void> {
        this.eventBus.publish(new ScriptCodeUpdatingEvent(id, code));

        try {
            return await super.updateCode(id, code, signal);
        } finally {
            this.eventBus.publish(new ScriptCodeUpdatedEvent(id, code));
        }
    }
}
