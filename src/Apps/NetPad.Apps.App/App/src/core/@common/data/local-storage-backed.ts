import {IObserver} from "@aurelia/runtime";
import {IDisposable, IObserverLocator, LifecycleFlags} from "aurelia";

export class LocalStorageBacked implements IDisposable {
    public readonly scope: string;
    protected disposables: (() => void)[] = [];

    constructor(scope: string) {
        if (!scope) throw new Error("Scope cannot be empty");
        if (scope.startsWith(".") || scope.endsWith("."))
            throw new Error("Scope should not start or end with a '.'");
        if (scope.startsWith(" ") || scope.endsWith(" "))
            throw new Error("Scope should not start or end with a space");

        this.scope = scope;
    }

    public load(): LocalStorageBacked {
        const valueStr = this.getItem("value");
        if (valueStr) {
            const value = JSON.parse(valueStr);

            if (value)
                Object.assign(this, value);
        }

        return this;
    }

    public save(o: unknown) {
        this.setItem("value", JSON.stringify(o));
    }

    public autoSave(observerLocator: IObserverLocator, propertiesThatTriggerAutoSave: string[]): LocalStorageBacked {
        const observers: IObserver[] = propertiesThatTriggerAutoSave.map(p => observerLocator.getObserver(this, p));

        const handler = {
            handleChange: (newValue: unknown, previousValue: unknown, flags: LifecycleFlags) => {
                if (newValue == previousValue) return;

                const newObj = { };
                for (const property of propertiesThatTriggerAutoSave) {
                    newObj[property] = this[property];
                }

                this.save(newObj);
            }
        };

        for (const observer of observers) {
            observer.subscribe(handler);
            this.disposables.push(() => observer.unsubscribe(handler));
        }

        return this;
    }

    public getItem(key: string) {
        return localStorage.getItem(this.getScopedKey(key));
    }

    public setItem(key: string, value: string) {
        return localStorage.setItem(this.getScopedKey(key), value);
    }

    private getScopedKey(key: string) {
        return `${this.scope}.${key}`
    }

    public dispose(): void {
        for (const disposable of this.disposables) {
            disposable();
        }
    }
}
