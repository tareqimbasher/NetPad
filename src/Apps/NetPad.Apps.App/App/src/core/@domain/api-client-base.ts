export class ApiClientBase {
    /**
     * When overriden, intercepts the fetch call
     * @param fetchCall the default fetch call
     * @protected
     */
    protected makeFetchCall(fetchCall: () => Promise<Response>): Promise<Response> {
        return fetchCall();
    }
}
