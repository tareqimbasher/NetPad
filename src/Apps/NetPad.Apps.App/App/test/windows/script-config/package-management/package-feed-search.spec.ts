import {ILogger} from "aurelia";
import {PackageSearchResults} from "../../../../src/core/@application/api";
import {IPackageService} from "../../../../src/core/@application/packages/ipackage-service";
import {PackageFeedSearch} from "../../../../src/windows/script-config/package-management/package-feed-search";

interface RecordedCall {
    term: string;
    skip: number;
    take: number;
    prerelease: boolean;
    signal?: AbortSignal;
}

type Responder = (call: RecordedCall) => Promise<PackageSearchResults>;

function page(ids: string[], hasMorePages = false, unavailableSources: string[] = []): PackageSearchResults {
    return PackageSearchResults.fromJS({
        packages: ids.map(id => ({packageId: id, title: id})),
        hasMorePages,
        unavailableSources,
    });
}

class FakePackageService {
    public readonly calls: RecordedCall[] = [];
    private readonly responders: Responder[] = [];

    public respondWith(...responders: Responder[]) {
        this.responders.push(...responders);
    }

    public search(
        term: string,
        skip: number,
        take: number,
        prerelease: boolean,
        signal?: AbortSignal
    ): Promise<PackageSearchResults> {
        const call: RecordedCall = {term, skip, take, prerelease, signal};
        this.calls.push(call);
        const responder = this.responders.shift() ?? (() => Promise.resolve(page([])));
        return responder(call);
    }
}

function createSearch(): {search: PackageFeedSearch; service: FakePackageService; errors: jest.Mock} {
    const service = new FakePackageService();
    const errors = jest.fn();
    const logger = {error: errors} as unknown as ILogger;
    return {
        search: new PackageFeedSearch(service as unknown as IPackageService, logger),
        service,
        errors,
    };
}

function ids(search: PackageFeedSearch): string[] {
    return search.results.map(r => r.packageId);
}

describe("PackageFeedSearch — paging", () => {
    it("takes hasMore from the envelope, not from the number of results", async () => {
        const {search, service} = createSearch();
        service.respondWith(() => Promise.resolve(page(["A", "B"], true)));

        await search.run(true);

        expect(ids(search)).toEqual(["A", "B"]);
        expect(search.hasMore).toBe(true);
    });

    it("reports no more pages even when a full-looking page came back", async () => {
        const {search, service} = createSearch();
        const full = Array.from({length: 60}, (_, i) => `P${i}`);
        service.respondWith(() => Promise.resolve(page(full, false)));

        await search.run(true);

        expect(search.results.length).toBe(60);
        expect(search.hasMore).toBe(false);
    });

    it("advances the cursor by the page size, not by the number of results", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page(Array.from({length: 60}, (_, i) => `P${i}`), true)),
            () => Promise.resolve(page(["Z"], false)),
        );

        await search.run(true);
        await search.loadMore();

        expect(service.calls.map(c => c.skip)).toEqual([0, 30]);
        expect(service.calls[0].take).toBe(30);
    });

    it("does not fetch another page when the last one was the last", async () => {
        const {search, service} = createSearch();
        service.respondWith(() => Promise.resolve(page(["A"], false)));

        await search.run(true);
        await search.loadMore();

        expect(service.calls.length).toBe(1);
    });

    it("resets results, cursor and error state on a fresh run", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page(["A"], true)),
            () => Promise.resolve(page(["B"], false)),
        );

        await search.run(true);
        await search.run(true);

        expect(ids(search)).toEqual(["B"]);
        expect(service.calls.map(c => c.skip)).toEqual([0, 0]);
    });
});

describe("PackageFeedSearch — appending", () => {
    it("drops packages already listed, whatever their casing", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page(["Newtonsoft.Json", "Serilog"], true)),
            () => Promise.resolve(page(["NEWTONSOFT.JSON", "Dapper"], false)),
        );

        await search.run(true);
        await search.loadMore();

        expect(ids(search)).toEqual(["Newtonsoft.Json", "Serilog", "Dapper"]);
    });

    it("keeps fetching while a page adds nothing and more pages exist", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page(["A"], true)),
            () => Promise.resolve(page(["A"], true)),
            () => Promise.resolve(page(["A"], true)),
            () => Promise.resolve(page(["B"], true)),
        );

        await search.run(true);
        await search.loadMore();

        expect(ids(search)).toEqual(["A", "B"]);
        expect(service.calls.map(c => c.skip)).toEqual([0, 30, 60, 90]);
    });

    it("gives up after a bounded number of all-duplicate pages", async () => {
        const {search, service} = createSearch();
        service.respondWith(...Array.from({length: 10}, () => () => Promise.resolve(page(["A"], true))));

        await search.run(true);
        await search.loadMore();

        expect(ids(search)).toEqual(["A"]);
        expect(service.calls.length).toBe(4);
        expect(search.hasMore).toBe(true);
        expect(search.loading).toBe(false);
    });

    it("stops pulling duplicate pages once the feeds are exhausted", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page(["A"], true)),
            () => Promise.resolve(page(["A"], false)),
        );

        await search.run(true);
        await search.loadMore();

        expect(service.calls.length).toBe(2);
        expect(search.hasMore).toBe(false);
    });
});

describe("PackageFeedSearch — superseded requests", () => {
    it("ignores a stale response that arrives after a newer one", async () => {
        const {search, service} = createSearch();
        let releaseStale!: () => void;
        service.respondWith(
            () => new Promise<PackageSearchResults>(resolve => {
                releaseStale = () => resolve(page(["STALE"], true));
            }),
            () => Promise.resolve(page(["FRESH"], false)),
        );

        const stale = search.run(true);
        const fresh = search.run(true);
        await fresh;
        releaseStale();
        await stale;

        expect(ids(search)).toEqual(["FRESH"]);
        expect(search.hasMore).toBe(false);
    });

    it("aborts the in-flight request and treats the abort as silence", async () => {
        const {search, service, errors} = createSearch();
        service.respondWith(
            call => new Promise<PackageSearchResults>((_, reject) => {
                call.signal?.addEventListener("abort", () => reject(new Error("AbortError")));
            }),
            () => Promise.resolve(page(["FRESH"], false)),
        );

        const aborted = search.run(true);
        const fresh = search.run(true);
        await Promise.all([aborted, fresh]);

        expect(service.calls[0].signal?.aborted).toBe(true);
        expect(search.error).toBe(false);
        expect(ids(search)).toEqual(["FRESH"]);
        expect(errors).not.toHaveBeenCalled();
    });

    it("aborts the in-flight request when disposed", async () => {
        const {search, service} = createSearch();
        service.respondWith(() => new Promise<PackageSearchResults>(() => {
            // never settles
        }));

        void search.run(true);
        search.dispose();

        expect(service.calls[0].signal?.aborted).toBe(true);
    });
});

describe("PackageFeedSearch — failures", () => {
    it("enters the error state when the search fails", async () => {
        const {search, service, errors} = createSearch();
        service.respondWith(() => Promise.reject(new Error("offline")));

        await search.run(true);

        expect(search.error).toBe(true);
        expect(search.hasMore).toBe(false);
        expect(search.loading).toBe(false);
        expect(errors).toHaveBeenCalled();
    });

    it("surfaces the sources that could not be searched", async () => {
        const {search, service} = createSearch();
        service.respondWith(() => Promise.resolve(page([], false, ["CPL Local", "Broken"])));

        await search.run(true);

        expect(search.unavailableSources).toEqual(["CPL Local", "Broken"]);
        expect(search.error).toBe(false);
    });

    it("clears previously unavailable sources on a fresh run", async () => {
        const {search, service} = createSearch();
        service.respondWith(
            () => Promise.resolve(page([], false, ["Broken"])),
            () => Promise.resolve(page(["A"], false)),
        );

        await search.run(true);
        await search.run(true);

        expect(search.unavailableSources).toEqual([]);
    });
});
