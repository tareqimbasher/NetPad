import {IHttpClient} from "@aurelia/fetch-client";
import {
    IEventBus,
    IScriptService,
    ScriptsApiClient
} from "@application";
import {ScriptCodeUpdatingEvent} from "@application/events/script-code-updating-event";
import {ScriptCodeUpdatedEvent} from "@application/events/script-code-updated-event";

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
