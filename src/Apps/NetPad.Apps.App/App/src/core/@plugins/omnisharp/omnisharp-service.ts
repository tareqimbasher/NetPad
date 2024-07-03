import {DI} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {IOmniSharpApiClient, OmniSharpApiClient} from "./api";
import {Semaphore, Util} from "@common";
import {IEventBus} from "@application";
import {ScriptCodeUpdatingEvent} from "@application/events/script-code-updating-event";
import {ScriptCodeUpdatedEvent} from "@application/events/script-code-updated-event";

export interface IOmniSharpService extends IOmniSharpApiClient {
}

export const IOmniSharpService = DI.createInterface<IOmniSharpService>();

export class OmniSharpService extends OmniSharpApiClient implements IOmniSharpService {
    // We want to limit omnisharp network calls to only a specific number at a time.
    // Monaco language provider can get pretty chatty and can end up clogging up
    // the browser's 6 network connection limit for a while. This results in
    // poor app responsiveness (ex, adding many new scripts quickly).
    private static semaphore = new Semaphore(3);

    // We want to wait while code buffer updates are in-process to ensure OmniSharp
    // calls are made against an updated document buffer.
    private currentCodeBufferUpdates = 0;

    constructor(@IEventBus private readonly eventBus: IEventBus, baseUrl?: string, @IHttpClient http?: IHttpClient) {
        super(baseUrl, http);
        this.eventBus.subscribe(ScriptCodeUpdatingEvent, msg => this.currentCodeBufferUpdates++);
        this.eventBus.subscribe(ScriptCodeUpdatedEvent, msg => this.currentCodeBufferUpdates--);
    }

    protected override async makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        while (this.currentCodeBufferUpdates > 0) {
            await Util.delay(10);
        }

        await OmniSharpService.semaphore.acquire();

        try {
            return await fetchCall();
        } catch (ex) {
            // Catch abort errors
            if (ex instanceof Error && ex.name?.startsWith("AbortError")) {
                return new Response(undefined);
            }
            throw ex;
        } finally {
            OmniSharpService.semaphore.release();
        }
    }
}

