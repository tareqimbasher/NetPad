import {observable} from '@aurelia/runtime';
import Split from "split.js";
import {IPackageService, CachedPackage, PackageMetadata, IAppService} from "@domain";

export class PackageManagement {
    @observable public term: string;
    public searchResults: PackageSearchResult[] = [];
    public cachedPackages: CachedPackage[] = [];
    public selectedPackage?: PackageMetadata;
    private disposables: (() => void)[] = [];

    constructor(
        @IPackageService readonly packageService: IPackageService,
        @IAppService readonly appService: IAppService
    ) {
    }

    public async attached() {
        const split = Split(["#cached-packages", "#package-search", "#package-info"], {
            gutterSize: 6,
            sizes: [35, 40, 25],
            minSize: [50, 50, 50],
        });
        this.disposables.push(() => split.destroy());

        await this.refreshCachedPackages();
        await this.searchPackages();
    }

    public dispose() {
        this.disposables.forEach(d => {
            try {
                d();
            }
            catch (ex) {
                console.error(ex);
            }
        });
    }

    public async termChanged(newValue: string, oldValue: string) {
        if (newValue === oldValue) return;
        await this.searchPackages(newValue);
    }

    public async searchPackages(term?: string) {
        const results = (await this.packageService.search(
            this.term,
            0,
            10,
            false)).map(r => new PackageSearchResult(r));

        this.markPackagesAddedToScript(results);

        this.searchResults = results;
    }

    public async addPackage(pkg: PackageMetadata) {
    }

    public async downloadPackage(pkg: PackageMetadata) {
        await this.packageService.download(pkg.packageId, pkg.version);
        await this.refreshCachedPackages();
    }

    public async deleteFromCache(pkg: PackageMetadata) {
        await this.packageService.deleteCachedPackage(pkg.packageId, pkg.version);
        this.cachedPackages.splice(this.cachedPackages.indexOf(pkg as CachedPackage), 1);
        this.markPackagesAddedToScript();
    }

    public async openCacheDirectory() {
        await this.appService.openPackageCacheFolder();
    }

    public selectPackage(pkg: PackageMetadata) {
        this.selectedPackage = pkg;
    }

    private async refreshCachedPackages() {
        this.cachedPackages = await this.packageService.getCachedPackages(true);
        this.markPackagesAddedToScript();
    }

    private markPackagesAddedToScript(searchResults?: PackageSearchResult[]) {
        searchResults ??= this.searchResults;

        for (const searchResult of searchResults) {
            searchResult.existsInLocalCache = !!this.cachedPackages.find(p => p.packageId === searchResult.packageId);
        }
    }
}

class PackageSearchResult extends PackageMetadata {
    public existsInLocalCache = false;
}
