/**
 * Handles fetching and storing a localStorage value.
 */
export class LocalStorageValue<TValue> {
    private _value: TValue | undefined;

    constructor(public readonly storageKey: string) {
    }

    public get value(): TValue | undefined {
        if (this._value !== undefined) {
            return this._value;
        }

        const json = localStorage.getItem(this.storageKey);
        if (!json) {
            this._value = undefined;
        } else {
            try {
                this._value = JSON.parse(json);
            } catch (e) {
                this._value = undefined;
            }
        }

        return this._value;
    }

    public set value(value: TValue | undefined) {
        this._value = value;
    }

    public save() {
        const value = this._value;
        if (value === undefined) {
            localStorage.removeItem(this.storageKey);
        } else {
            localStorage.setItem(this.storageKey, JSON.stringify(value));
        }
    }
}
