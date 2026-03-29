import {DI} from "aurelia";
import {IHttpClient} from "@aurelia/fetch-client";
import {IOmniSharpApiClient, OmniSharpApiClient, OmniSharpAsyncBufferUpdateCompletedEvent} from "./api";
import {Semaphore, Util} from "@common";
import {IEventBus} from "@application";
import {ScriptCodeUpdatingEvent} from "@application/scripts/script-code-updating-event";

export interface IOmniSharpService extends IOmniSharpApiClient {
}

export const IOmniSharpService = DI.createInterface<IOmniSharpService>();

export class OmniSharpService extends OmniSharpApiClient implements IOmniSharpService {
    // We want to limit omnisharp network calls to only a specific number at a time.
    // Monaco language provider can get pretty chatty and can end up clogging up
    // the browser's 6 network connection limit for a while. This results in
    // poor app responsiveness (ex, adding many new scripts quickly).
    private static semaphore = new Semaphore(3);

    // Gate that delays OmniSharp API calls until the server has processed all pending
    // buffer updates. Without this, requests can hit stale buffer state and produce
    // ArgumentOutOfRangeException on line/column positions.
    private pendingBufferUpdates = 0;
    private bufferSyncPromise: Promise<void> | null = null;
    private bufferSyncResolve: (() => void) | null = null;

    constructor(@IEventBus private readonly eventBus: IEventBus, baseUrl?: string, @IHttpClient http?: IHttpClient) {
        super(baseUrl, http);

        // Track each code update heading to the backend. A single shared promise is
        // created on the first update and kept open until all updates are confirmed.
        this.eventBus.subscribe(ScriptCodeUpdatingEvent, () => {
            this.pendingBufferUpdates++;
            if (!this.bufferSyncPromise) {
                this.bufferSyncPromise = new Promise<void>(resolve => {
                    this.bufferSyncResolve = resolve;
                });
            }
        });

        // The server publishes this after OmniSharp has actually ingested the buffer.
        // Only open the gate once every pending update has a matching completion.
        this.eventBus.subscribeToServer(OmniSharpAsyncBufferUpdateCompletedEvent, () => {
            if (this.pendingBufferUpdates > 0) {
                this.pendingBufferUpdates--;
            }
            if (this.pendingBufferUpdates === 0) {
                this.bufferSyncResolve?.();
                this.bufferSyncResolve = null;
                this.bufferSyncPromise = null;
            }
        });
    }

    protected override async makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        if (this.bufferSyncPromise) {
            const gate = this.bufferSyncPromise;
            let resolved = false;
            await Promise.race([
                gate.then(() => { resolved = true; }),
                Util.delay(3000)
            ]);
            // Self-heal: if the timeout fired (e.g. backend failed to publish the
            // completion event), reset state so subsequent calls aren't permanently delayed.
            if (!resolved && this.bufferSyncPromise === gate) {
                this.pendingBufferUpdates = 0;
                this.bufferSyncResolve?.();
                this.bufferSyncResolve = null;
                this.bufferSyncPromise = null;
            }
        }

        await OmniSharpService.semaphore.acquire();

        try {
            return await fetchCall();
        } catch (ex) {
            if (ex instanceof Error && ex.name?.startsWith("AbortError")) {
                return new Response(null, { status: 204 });
            }
            throw ex;
        } finally {
            OmniSharpService.semaphore.release();
        }
    }
}
