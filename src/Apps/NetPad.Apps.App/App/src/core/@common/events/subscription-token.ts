import {IDisposable} from "@common";

export class SubscriptionToken implements IDisposable {
    private disposeAction: (() => void) | null;
    private isDisposed = false;

    constructor(disposeAction: () => void) {
        this.disposeAction = disposeAction;
    }

    public dispose(): void {
        if (this.isDisposed) return;

        try{
            if (this.disposeAction) {
                this.disposeAction();
            }
        } finally {
            this.isDisposed = true;
            this.disposeAction = null;
        }
    }
}
