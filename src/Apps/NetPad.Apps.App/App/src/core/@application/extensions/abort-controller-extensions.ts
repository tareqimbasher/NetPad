import {CancellationToken, CancellationTokenSource} from "monaco-editor";

declare global {
    interface AbortController {
        /**
         * The default timeout.
         */
        defaultTimeout: number;

        /**
         * Creates an abort signal from a Cancellation Token.
         * @param token The token to track. When this token is cancelled the signal is aborted.
         * @param timeout If specified, will abort signal after specified timeout (in milliseconds).
         */
        signalFrom(token: CancellationToken, timeout?: number): AbortSignal;

        /**
         * Creates an abort signal that will abort after the specified timeout (in milliseconds).
         * @param timeout Will abort signal after specified timeout (in milliseconds).
         */
        signalFrom(timeout: number): AbortSignal;

        /**
         * Creates an abort signal that will abort after the specified timeout (in milliseconds).
         */
        signalFromDefaultTimeout(): AbortSignal;
    }
}

AbortController.prototype.defaultTimeout = 10000;

AbortController.prototype.signalFromDefaultTimeout = function (this: AbortController) {
    return this.signalFrom(this.defaultTimeout);
}

AbortController.prototype.signalFrom = function (this: AbortController, tokenOrTimeout: CancellationToken | number | undefined, timeout?: number) {
    let token: CancellationToken | undefined;

    if (!tokenOrTimeout) {
        timeout = this.defaultTimeout;
    }
    else if (typeof(tokenOrTimeout) === "number") {
        timeout = tokenOrTimeout;
    } else {
        token = tokenOrTimeout;
    }

    if (!timeout) timeout = this.defaultTimeout;

    if (token) {
        const cts = new CancellationTokenSource(token);
        token = cts.token;

        const timeoutHandle = setTimeout(() => cts.cancel(), timeout);

        token.onCancellationRequested(() => {
            clearTimeout(timeoutHandle);
            this.abort();
        });
    }
    else {
        setTimeout(() => this.abort(), timeout);
    }

    return this.signal;
};
