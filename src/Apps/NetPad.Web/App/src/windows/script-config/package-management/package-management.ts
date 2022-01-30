import {observable} from '@aurelia/runtime';
import Split from "split.js";
import {IPackageService, CachedPackage, PackageMetadata, IAppService, PackageReference} from "@domain";
import {ConfigStore} from "../config-store";

export class PackageManagement {
    @observable public term: string;
    public searchResults: PackageSearchResult[] = [];
    public cachedPackages: CachedPackageViewModel[] = [];
    public selectedPackage?: PackageMetadata;

    public searchLoadingPromise?: Promise<void>;
    public cacheLoadingPromise?: Promise<void>;
    private disposables: (() => void)[] = [];

    constructor(
        readonly configStore: ConfigStore,
        @IPackageService readonly packageService: IPackageService,
        @IAppService readonly appService: IAppService
    ) {
    }

    public attached() {
        // HACK: not sure why destroying the split isn't removing the gutter
        document.querySelectorAll("package-management .gutter").forEach(e => e.remove());

        const split = Split(["#cached-packages", "#package-search", "#package-info"], {
            gutterSize: 6,
            sizes: [35, 40, 25],
            minSize: [50, 50, 50],
        });
        this.disposables.push(() => split.destroy());

        this.refreshCachedPackages();
        this.searchPackages();
    }

    public dispose() {
        this.disposables.forEach(d => d());
    }

    public async termChanged(newValue: string, oldValue: string) {
        if (newValue === oldValue) return;
        await this.searchPackages(newValue);
    }

    public async referencePackage(pkg: PackageSearchResult | CachedPackageViewModel) {
        if (pkg.referenced)
            return;

        if (pkg instanceof PackageSearchResult && !pkg.existsInLocalCache)
            await this.downloadPackage(pkg);

        this.configStore.references.push(new PackageReference({
            packageId: pkg.packageId,
            title: pkg.title,
            version: pkg.version
        }));
        this.markReferencedPackages();
    }

    public async referenceSpecificPackageVersion(pkg: PackageReference) {
        alert("Not implemented yet.");
    }

    public async downloadPackage(pkg: PackageSearchResult) {
        try {
            pkg.isDownloading = true;
            await this.packageService.download(pkg.packageId, pkg.version);
            await this.refreshCachedPackages();
        } catch (ex) {
            alert(`Download failed. ${ex.message}`);
        } finally {
            pkg.isDownloading = false;
        }
    }

    public async deleteFromCache(pkg: PackageMetadata) {
        await this.packageService.deleteCachedPackage(pkg.packageId, pkg.version);
        this.cachedPackages.splice(this.cachedPackages.indexOf(pkg as CachedPackageViewModel), 1);
        this.markReferencedPackages();
    }

    public async openCacheDirectory() {
        await this.appService.openPackageCacheFolder();
    }

    private async refreshCachedPackages() {
        this.cacheLoadingPromise = this.packageService.getCachedPackages(true)
            .then(cps => {
                this.cachedPackages = cps.map(p => new CachedPackageViewModel(p));
                this.markReferencedPackages();
            });
    }

    private async searchPackages(term?: string) {
        this.searchLoadingPromise = this.packageService.search(
            this.term,
            0,
            10,
            false)
            .then(data => {
                const results = data.map(r => new PackageSearchResult(r));
                this.markReferencedPackages(results);
                this.searchResults = results;
            });
    }

    private markReferencedPackages(searchResults?: PackageSearchResult[]) {
        searchResults ??= this.searchResults;

        for (const searchResult of searchResults) {
            searchResult.existsInLocalCache = !!this.cachedPackages
                .find(p => p.packageId === searchResult.packageId);
            searchResult.referenced = !!this.configStore.references
                .find(r => (r as PackageReference).packageId == searchResult.packageId);
        }

        for (const cachedPackage of this.cachedPackages) {
            cachedPackage.referenced = !!this.configStore.references
                .find(r => (r as PackageReference).packageId == cachedPackage.packageId);
        }
    }
}

class PackageSearchResult extends PackageMetadata {
    public existsInLocalCache = false;
    public isDownloading = false;
    public referenced = false;
}

class CachedPackageViewModel extends CachedPackage {
    public referenced = false;
}
