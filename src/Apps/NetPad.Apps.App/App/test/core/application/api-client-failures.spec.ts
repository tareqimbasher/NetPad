import {IHttpClient} from "@aurelia/fetch-client";
import {ApiException, PackagesApiClient} from "@application/api";

const clientWhoseFetch = (fetch: () => Promise<unknown>) =>
    new PackagesApiClient("", {fetch} as unknown as IHttpClient);

const callClient = (client: PackagesApiClient) => client.getPackageMetadata([]);

describe("generated API client failures", () => {
    test("an aborted request reaches the caller unchanged", async () => {
        const abort = new DOMException("The operation was aborted.", "AbortError");

        await expect(callClient(clientWhoseFetch(() => Promise.reject(abort)))).rejects.toBe(abort);
    });

    test("a network failure reaches the caller unchanged", async () => {
        const networkError = new TypeError("Failed to fetch");

        await expect(callClient(clientWhoseFetch(() => Promise.reject(networkError)))).rejects.toBe(networkError);
    });

    test("a resolved 500 becomes an ApiException carrying the backend's body", async () => {
        const body = JSON.stringify({message: "Feed unavailable."});
        const response = {status: 500, headers: new Map(), text: () => Promise.resolve(body)};

        const error = await callClient(clientWhoseFetch(() => Promise.resolve(response))).catch(e => e);

        expect(error).toBeInstanceOf(ApiException);
        expect(error.status).toBe(500);
        expect(error.response).toBe(body);
    });
});
