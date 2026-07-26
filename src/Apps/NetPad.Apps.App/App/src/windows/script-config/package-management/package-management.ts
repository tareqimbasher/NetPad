import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import Split from "split.js";
import {IAppService, IPackageService, PackageReference, splitterGutterSize, ViewModelBase} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {ConfigStore} from "../config-store";
import {PackageFeedSearch} from "./package-feed-search";
import {PackageCache} from "./package-cache";
import {CachedPackageViewModel, PackageRowViewModel, PackageSelection} from "./package-view-models";

type PackageSource = "feeds" | "cache";

/** Local storage key used to persists the list/detail divider position. */
const SPLIT_STORAGE_KEY = "script-config.package-browser.split";

/**
 * Orchestrates the package browser. The two sources ({@link PackageFeedSearch}, {@link PackageCache}) and
 * the detail panel each own their own state.
 */
export class PackageManagement extends ViewModelBase {
    public source: PackageSource = "feeds";

    public readonly search: PackageFeedSearch;
    public readonly cache: PackageCache;

    public selection?: PackageSelection;

    public listPaneEl!: HTMLElement;
    public detailPaneEl!: HTMLElement;
    private split?: Split.Instance;

    constructor(
        readonly configStore: ConfigStore,
        @IPackageService readonly packageService: IPackageService,
        @IAppService readonly appService: IAppService,
        private readonly dialogUtil: DialogUtil,
        @ILogger logger: ILogger
    ) {
        super(logger);
        this.search = new PackageFeedSearch(packageService, this.logger);
        this.cache = new PackageCache(packageService);
    }

    public attached() {
        this.cache.refresh();
        this.search.run(true);

        let sizes: number[] | undefined;
        try {
            const saved = localStorage.getItem(SPLIT_STORAGE_KEY);
            if (saved) sizes = JSON.parse(saved);
        } catch {
            // ignore
        }

        this.split = Split([this.listPaneEl, this.detailPaneEl], {
            sizes: sizes ?? [72, 28],
            minSize: [320, 280],
            gutterSize: splitterGutterSize,
            onDragEnd: s => localStorage.setItem(SPLIT_STORAGE_KEY, JSON.stringify(s)),
        });
    }

    protected override detaching() {
        this.split?.destroy();
        this.search.dispose();
        this.cache.dispose();
        super.detaching();
    }

    public get visibleRows(): PackageRowViewModel[] {
        return this.source === "feeds" ? this.search.results : this.cache.visible;
    }

    public get cacheCount(): number {
        return this.cache.packages.length;
    }

    public setSource(source: PackageSource) {
        if (this.source === source) return;
        this.source = source;
        this.selection = undefined;

        if (source === "feeds" && this.search.results.length === 0) {
            this.search.run(true);
        }
    }

    public selectRow(row: PackageRowViewModel) {
        // A cache row is a specific version, so it targets its version, a feeds row targets latest.
        const version = row instanceof CachedPackageViewModel ? row.version : undefined;
        this.selection = {package: row, version};
    }

    public onListScroll(event: Event) {
        if (this.source !== "feeds") return;

        const el = event.target as HTMLElement;
        if (el.scrollTop + el.clientHeight >= el.scrollHeight - 48) {
            this.search.loadMore();
        }
    }

    @watch<PackageManagement>(vm => vm.search.term)
    private searchTermChanged() {
        this.search.run(true);
    }

    @watch<PackageManagement>(vm => vm.search.prerelease)
    private searchPrereleaseChanged() {
        this.search.run(true);
    }

    // The row "referenced" chips reflect the script's references. Restamp whenever the references,
    // the search page, or the cache contents change.
    @watch<PackageManagement>(vm => vm.configStore.references.length)
    @watch<PackageManagement>(vm => vm.search.results)
    private stampReferences() {
        const refs = this.configStore.references.filter(r => r instanceof PackageReference) as PackageReference[];
        for (const result of this.search.results) {
            result.referencedVersion = refs.find(r => r.packageId === result.packageId)?.version;
        }
        this.cache.applyReferences(refs);
    }

    // A cache refresh rebuilds all row objects, so a cache-source selection must be re-pointed
    // at the fresh object, or cleared when the version is gone (ex: after delete/purge).
    @watch<PackageManagement>(vm => vm.cache.packages)
    private onCachePackagesChanged() {
        if (this.source === "cache" && this.selection) {
            const sel = this.selection;
            const match = this.cache.packages.find(p => p.packageId === sel.package.packageId && p.version === sel.version);
            this.selection = match ? {package: match, version: sel.version} : undefined;
        }
        this.stampReferences();
    }

    public async openCacheDirectory() {
        await this.appService.openPackageCacheFolder();
    }

    public async purgeCache() {
        const confirmation = await this.dialogUtil.ask({
            message: "Delete every downloaded package? Packages your scripts reference are downloaded again the next time they run."
        });
        if (confirmation.value !== "OK") return;

        await this.packageService.purgePackageCache();
        await this.cache.refresh();
    }
}
