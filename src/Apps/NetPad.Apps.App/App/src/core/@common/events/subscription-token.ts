import {IDisposable} from "aurelia";

export class SubscriptionToken implements IDisposable {
    constructor(private readonly action: () => void) {
    }

    public dispose(): void {
        this.action();
    }
}
