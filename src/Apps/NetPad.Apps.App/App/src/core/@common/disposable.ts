export interface IDisposable {
    dispose(): void;
}

export abstract class WithDisposables implements IDisposable {
    private readonly disposables: (() => void)[] = [];

    public addDisposable(disposable: IDisposable | (() => void)) {
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
                if (disposable !== undefined) {
                    disposable();
                }
            } catch (ex) {
                console.error("Error while disposing", disposable, ex);
            }
        }
    }
}
