import {observable} from '@aurelia/runtime';
import {IPackageService, CachedPackage, PackageMetadata} from "@domain";

export class PackageManagement {
    @observable public term: string;
    public searchResults: PackageSearchResult[] = [];
    public cachedPackages: CachedPackage[] = [];

    constructor(@IPackageService readonly packageService: IPackageService) {
    }

    public async attached() {
        await this.refreshCachedPackages();
    }

    public async termChanged(newValue: string, oldValue: string) {
        if (newValue === oldValue) return;

        const results = (await this.packageService.search(
            this.term,
            0,
            10,
            false)).map(r => new PackageSearchResult(r));

        for (const result of results) {
            result.existsInLocalCache = !!this.cachedPackages.find(p => p.packageId === result.packageId);
        }

        this.searchResults = results;
    }

    public async addPackage(pck: PackageMetadata) {

    }

    public async downloadPackage(pck: PackageMetadata) {
        await this.packageService.download(pck.packageId, pck.versions[pck.versions.length - 1]);
        await this.refreshCachedPackages();
        alert("Download complete");
    }

    public async deleteFromCache(pck: PackageMetadata) {

    }

    private async refreshCachedPackages() {
        this.cachedPackages = await this.packageService.getCachedPackages(true);
    }
}

class PackageSearchResult extends PackageMetadata {
    public existsInLocalCache = false;
}
