import {CancellationToken} from "monaco-editor";

declare global {
    interface AbortController {
        signalFrom(token: CancellationToken): AbortSignal;
    }
}

AbortController.prototype.signalFrom = function (this: AbortController, token: CancellationToken) {
    token.onCancellationRequested(() => this.abort());
    return this.signal;
};
