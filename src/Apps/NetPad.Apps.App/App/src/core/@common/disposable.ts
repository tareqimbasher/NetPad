export interface IDisposable {
    dispose(): void;
}

export class DisposableCollection implements IDisposable {
    private readonly disposables: (() => void)[] = [];

    public add(disposable: IDisposable | (() => void)) {
        if (disposable instanceof Function) {
            this.disposables.push(disposable);
        } else {
            this.disposables.push(() => disposable.dispose());
        }
    }

    public dispose() {
        while (this.disposables.length > 0) {
            const disposable = this.disposables.shift();

            try {
                if (typeof disposable === "function") {
                    disposable();
                }
            } catch (ex) {
                console.error("Error while disposing", disposable, ex);
            }
        }
    }
}

export abstract class WithDisposables implements IDisposable {
    private readonly disposables = new DisposableCollection();

    public addDisposable(disposable: IDisposable | (() => void)) {
        this.disposables.add(disposable);
    }

    public dispose() {
        this.disposables.dispose();
    }
}
