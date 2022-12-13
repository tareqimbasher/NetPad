import {ILogger} from "aurelia";
import {SubscriptionToken} from "@common";

export class AppMutationObserver {
    private mutationObserver?: MutationObserver;
    private readonly listeners: MutationCallback[];
    private readonly logger: ILogger;

    constructor(@ILogger logger) {
        this.logger = logger.scopeTo(nameof(AppMutationObserver));
        this.listeners = [];
    }

    public subscribe(callback: MutationCallback): SubscriptionToken {
        if (!callback)
            throw new Error("Callback cannot be null or undefined");

        if (!this.mutationObserver) {
            this.mutationObserver = this.initializeMutationObserver();
        }

        const firstListener = this.listeners.length === 0;

        this.listeners.push(callback);

        // Start observing mutations when there are listeners
        if (firstListener) {
            this.mutationObserver.observe(document, {
                childList: true,
                subtree: true
            });
        }

        return new SubscriptionToken(() => this.unsubscribe(callback));
    }

    private unsubscribe(callback: MutationCallback) {
        const ix = this.listeners.indexOf(callback);
        if (ix >= 0)
            this.listeners.splice(ix, 1);

        // Stop observing mutations if no more listeners exist
        if (this.listeners.length === 0) {
            this.mutationObserver?.disconnect();
        }
    }

    private initializeMutationObserver() {
        return new MutationObserver(
            (mutations, observer) => this.mutationCallback(mutations, observer));
    }

    private mutationCallback(mutations: MutationRecord[], observer: MutationObserver): void {
        for (const listener of this.listeners) {
            try {
                listener(mutations, observer);
            }
            catch (ex) {
                this.logger.error("Error while executing mutation callback for listener", listener);
            }
        }
    }
}
