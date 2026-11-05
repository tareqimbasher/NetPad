import {ILogger} from "aurelia";
import {PackageMetadata} from "@application/api";
import {IPackageService} from "@application/packages/ipackage-service";
import {SearchResultViewModel} from "./package-view-models";

/**
 * How many consecutive pages of nothing-new to pull before stopping.
 */
const maxAutoContinuedPages = 3;

/**
 * The Feeds source: a paged NuGet search.
 */
export class PackageFeedSearch {
    public term = "";
    public prerelease = false;
    public results: SearchResultViewModel[] = [];
    public loading = false;
    public error = false;
    public hasMore = false;
    public unavailableSources: string[] = [];

    private skip = 0;
    private readonly take = 30;
    private readonly loadedIds = new Set<string>();
    // Guards against a slower earlier request overwriting a newer one's results.
    private seq = 0;
    private inFlight?: AbortController;

    constructor(
        private readonly packageService: IPackageService,
        private readonly logger: ILogger
    ) {
    }

    public loadMore(): Promise<void> {
        return this.run(false);
    }

    public async run(reset: boolean): Promise<void> {
        if (reset) {
            this.skip = 0;
            this.results = [];
            this.loadedIds.clear();
            this.hasMore = false;
            this.error = false;
            this.unavailableSources = [];
        } else if (this.loading || !this.hasMore) {
            return;
        }

        const seq = ++this.seq;
        this.abort();
        const inFlight = new AbortController();
        this.inFlight = inFlight;
        this.loading = true;

        try {
            // A page can be entirely packages we already show (several sources serving the same
            // package, split across a page boundary).
            for (let page = 0; page < maxAutoContinuedPages; page++) {
                const results = await this.packageService.search(
                    this.term, this.skip, this.take, this.prerelease, inFlight.signal);

                if (seq !== this.seq) return;

                this.skip += this.take;
                this.hasMore = results.hasMorePages;
                this.unavailableSources = results.unavailableSources;

                if (this.append(results.packages) > 0 || !this.hasMore) {
                    break;
                }
            }
        } catch (ex) {
            if (seq !== this.seq || inFlight.signal.aborted) return;
            this.error = true;
            this.hasMore = false;
            this.logger.error("Package search failed", ex);
        } finally {
            if (seq === this.seq) {
                this.loading = false;
                this.inFlight = undefined;
            }
        }
    }

    public dispose() {
        this.abort();
    }

    /** Appends the packages not already listed, and returns how many that was. */
    private append(packages: PackageMetadata[]): number {
        const added: SearchResultViewModel[] = [];

        for (const pkg of packages) {
            const id = pkg.packageId.toLowerCase();
            if (this.loadedIds.has(id)) continue;
            this.loadedIds.add(id);
            added.push(Object.assign(new SearchResultViewModel(), pkg));
        }

        if (added.length > 0) {
            this.results = [...this.results, ...added];
        }

        return added.length;
    }

    private abort() {
        this.inFlight?.abort();
        this.inFlight = undefined;
    }
}
