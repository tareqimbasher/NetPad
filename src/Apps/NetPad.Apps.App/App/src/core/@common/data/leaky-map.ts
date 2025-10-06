/**
 * A map that will automatically delete keys that have not been accessed for a certain amount of time (the timeout).
 *
 * When a key is added, a sliding timer is started for that key. When that key's timer has elapsed the key will be
 * removed from the map. Using `get()` or `set()` will reset the key's timer.
 */
export class LeakyMap<K, V> extends Map<K, V> {
    private timeoutMap = new Map<K, ReturnType<typeof setTimeout>>();

    constructor(private readonly timeout: number) {
        super();
        if (!Number.isFinite(timeout) || timeout < 0) {
            throw new TypeError(`timeout must be a non-negative finite number; got ${timeout}`);
        }
    }

    private resetTimeoutIfPresent(key: K) {
        if (!super.has(key)) return;

        const handle = this.timeoutMap.get(key);
        if (handle) clearTimeout(handle);

        const newHandle = setTimeout(() => this.delete(key), this.timeout);
        this.timeoutMap.set(key, newHandle);
    }

    public override get(key: K): V | undefined {
        if (super.has(key)) this.resetTimeoutIfPresent(key);
        return super.get(key);
    }

    public override set(key: K, value: V): this {
        super.set(key, value);
        this.resetTimeoutIfPresent(key);
        return this;
    }

    public override delete(key: K): boolean {
        const handle = this.timeoutMap.get(key);
        if (handle) {
            clearTimeout(handle);
            this.timeoutMap.delete(key);
        }
        return super.delete(key);
    }

    public override clear(): void {
        this.timeoutMap.forEach((h) => clearTimeout(h));
        this.timeoutMap.clear();
        super.clear();
    }
}
