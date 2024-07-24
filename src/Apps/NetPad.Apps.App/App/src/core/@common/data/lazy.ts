/**
 * Lazily initializes and then caches a value. The value is initialized only once, when it is first accessed.
 */
export class Lazy<T> {
    private _isInitialized = false;
    private _value: T;

    constructor(private readonly initializer: () => T) {
    }

    public get isInitialized(): boolean {
        return this._isInitialized;
    }

    public get value(): T {
        if (!this._isInitialized) {
            this._value = this.initializer();
            this._isInitialized = true;
        }

        return this._value;
    }
}
