export class Lazy<T> {
    private _value?: T;

    constructor(private readonly initializer: () => T) {
    }

    get value(): T {
        if (!this._value)
            this._value = this.initializer();

        return this._value;
    }
}
