import {DataConnectionsApiClient, IDataConnectionService} from "@application";
import {expectedFailureHeader} from "@application/api-client-base";

export class DataConnectionService extends DataConnectionsApiClient implements IDataConnectionService {
    /**
     * Enumerating the databases on a server is expected to fail when the user is still filling in
     * connection info (like auth).
     */
    protected override makeFetchCall(url: string, options: RequestInit, fetchCall: () => Promise<Response>): Promise<Response> {
        if (url.endsWith("/databases")) {
            (options.headers as Record<string, string>)[expectedFailureHeader] = "true";
        }

        return super.makeFetchCall(url, options, fetchCall);
    }
}
