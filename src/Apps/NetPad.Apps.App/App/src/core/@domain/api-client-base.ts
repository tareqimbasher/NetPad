export class ApiClientBase {
    /**
     * When overriden, intercepts the fetch call.
     * @param url Request url.
     * @param options Request options.
     * @param fetchCall The default fetch call.
     * @protected
     */
    protected makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        return fetchCall();
    }
}
