/**
 * A map that will automatically delete keys that have not been accessed for a certain amount of time (the timeout).
 *
 * When a key is added, a sliding timer is started for that key. When that key's timer has elapsed the key will be
 * removed from the map. Using `get()` or `set()` will reset the key's timer.
 */
export class LeakyMap<K, V> extends Map<K, V> {
    private timeoutMap = new Map<K, NodeJS.Timeout>();

    constructor(private readonly timeout: number) {
        super();
    }

    private resetTimeout(key: K) {
        const handle = this.timeoutMap.get(key);

        if (handle) {
            clearTimeout(handle);
        }

        this.timeoutMap.set(key, setTimeout(() => super.delete(key), this.timeout));
    }

    public override get(key: K): V | undefined {
        this.resetTimeout(key);
        return super.get(key);
    }

    public override set(key: K, value: V) {
        super.set(key, value);
        this.resetTimeout(key);
        return this;
    }

    public override delete(key: K) {
        const handle = this.timeoutMap.get(key);
        if (handle) {
            clearTimeout(handle);
            this.timeoutMap.delete(key);
        }
        return super.delete(key);
    }

    public override clear() {
        super.clear();
        this.timeoutMap.forEach((handle) => clearTimeout(handle));
        this.timeoutMap.clear();
    }
}
