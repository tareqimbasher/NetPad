export interface IDisposable {
    dispose(): void;
}

export abstract class WithDisposables implements IDisposable {
    private disposables: (() => void)[] = [];

    public addDisposable(disposable: IDisposable | (() => void)) {
        if (disposable instanceof Function) {
            this.disposables.push(disposable);
        } else {
            this.disposables.push(() => disposable.dispose());
        }
    }

    public dispose() {
        let disposable = this.disposables.pop();

        while (disposable) {
            try {
                disposable();
            } catch (ex) {
                console.error("Error while disposing", disposable, ex);
            }

            disposable = this.disposables.pop();
        }
    }
}
