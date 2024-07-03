export interface IBufferedQueueOptions<T> {
    /**
     * Time in milliseconds after which to flush the queue. If not set, the queue is not flushed.
     */
    flushOnInterval?: number;

    /**
     * Minimum number of items to aggregate before flushing the queue. The queue can contain more than
     * this number before it is flushed.
     */
    flushOnSize?: number;

    /**
     * The action to execute when the queue is flushed.
     * @param items The items that are removed from the queue.
     */
    onFlush(items: T[]): Promise<void>;
}

/**
 * A collection that gets flushed when a threshold is reached as defined by the options.
 */
export class BufferedQueue<T> {
    private items: T[];
    private timeoutHandle: NodeJS.Timeout | undefined;

    constructor(private options: IBufferedQueueOptions<T>) {
        if (!options.onFlush) {
            throw new Error(`Option ${nameof(options, "onFlush")} is not defined. This function must be defined.`);
        }

        this.items = [];

        this.startTimerIfApplicable();
    }

    /**
     * Adds an item to the queue buffer.
     * @param item The item to add
     */
    public add(item: T) {
        this.items.push(item);

        // If threshold is reached
        if (this.options.flushOnSize && this.options.flushOnSize > 1 && this.items.length > this.options.flushOnSize) {
            this.flush();
        }
    }

    /**
     * Flush the queue, effectively removing all items.
     */
    public flush() {
        if (this.timeoutHandle) {
            // Stop the timer. We will restart it after flushing completes.
            // Clearing the timeout here means that the timer is stopped, then restarted later, every time flush is called
            // This could happen if size limit is (configured and) reached for example.
            // In other words, if the size limit is reached, or if flush is called manually, the timer is reset.
            clearTimeout(this.timeoutHandle);
            this.timeoutHandle = undefined;
        }

        let promise = Promise.resolve();

        try {
            const flushed = this.items.splice(0);

            if (flushed.length > 0) {
                promise = this.options.onFlush(flushed);
            }

        } finally {
            promise.then(() => this.startTimerIfApplicable());
        }
    }

    private startTimerIfApplicable() {
        if (!this.timeoutHandle && this.options.flushOnInterval && this.options.flushOnInterval > 1) {
            this.timeoutHandle = setTimeout(() => this.flush(), this.options.flushOnInterval);
        }
    }
}
