export interface IDisposable {
    dispose(): void;
}

export abstract class WithDisposables implements IDisposable {
    private disposables: (() => void)[] = [];

    protected addDisposable(disposable: IDisposable | (() => void)) {
        if (disposable instanceof Function) {
            this.disposables.push(disposable);
        } else {
            this.disposables.push(() => disposable.dispose());
        }
    }

    public dispose() {
        let disposable: (() => void) | undefined | null;

        while (disposable = this.disposables.pop()) {
            try {
                disposable();
            } catch (ex) {
                console.error("Error while disposing", disposable, ex);
            }
        }
    }
}
