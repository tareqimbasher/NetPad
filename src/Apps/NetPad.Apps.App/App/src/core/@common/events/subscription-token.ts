import {IDisposable} from "aurelia";

export class SubscriptionToken implements IDisposable {
    constructor(private readonly disposeAction: () => void) {
    }

    public dispose(): void {
        this.disposeAction();
    }
}
