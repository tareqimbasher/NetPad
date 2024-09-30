export class ApiClientBase {
    /**
     * When overriden, intercepts the fetch call an ApiClient makes.
     * @param url Request url.
     * @param options Request options.
     * @param fetchCall The default fetch call.
     * @protected
     */
    protected makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        return fetchCall().then(r => r).catch(r => r);

        // Return response in both success or error so that "process()" functions in api.ts can decide what to do with it
        // Otherwise when an error occurs "process()" functions don't get called and error propagates to caller without
        // proper handling.
        //return fetchCall().then(r => r).catch(r => r);
    }
}
