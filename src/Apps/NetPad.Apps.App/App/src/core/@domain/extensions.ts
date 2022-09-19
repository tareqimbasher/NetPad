import {CancellationToken} from "monaco-editor";

declare global
{
    interface AbortController
    {
        signalFrom(token: CancellationToken): AbortSignal;
    }
}

AbortController.prototype.signalFrom = function (this: AbortController, token: CancellationToken)
{
    const controller = this;
    token.onCancellationRequested(() => controller.abort());
    return controller.signal;
};
