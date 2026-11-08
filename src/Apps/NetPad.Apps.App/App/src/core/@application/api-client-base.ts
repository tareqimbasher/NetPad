/**
 * Marks a request whose failure is expected, so the HTTP client logs it as debug instead of as an error.
 */
export const expectedFailureHeader = "X-NetPad-Expected-Failure";

export class ApiClientBase {
    /**
     * When overridden, intercepts the fetch call an ApiClient makes.
     * @param url Request url.
     * @param options Request options.
     * @param fetchCall The default fetch call.
     * @protected
     */
    protected makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        return fetchCall();
    }
}
