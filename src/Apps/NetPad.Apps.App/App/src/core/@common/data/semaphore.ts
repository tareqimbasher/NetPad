/**
 * Allows restricting concurrent access to a block of code to a limited number of
 * accessors at once.
 */
export class Semaphore {
    private count: number;
    private queue: (() => void)[];

    constructor(private limit: number) {
        this.count = 0;
        this.queue = [];
    }

    public async acquire() {
        if (this.count < this.limit) {
            this.count++;
            return;
        }

        await new Promise<void>(resolve => this.queue.push(resolve));
        this.count++;
    }

    public release() {
        if (this.count === 0) {
            throw new Error("Semaphore count cannot be negative");
        }

        this.count--;

        if (this.queue.length > 0) {
            const next = this.queue.shift();
            if (next) {
                next();
            }
        }
    }
}
