import Split from "split.js";
import {CachedPackage, IAppService, IPackageService, PackageMetadata, PackageReference} from "@domain";
import {ConfigStore} from "../config-store";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {ViewModelBase} from "@application";

export class PackageManagement extends ViewModelBase {
    public searchTerm: string;
    public searchTake = 10;
    public searchPrereleases = false;
    public searchCurrentPage = 1;

    public searchResults: PackageSearchResult[] = [];
    public cachedPackages: CachedPackageViewModel[] = [];
    public selectedPackage?: PackageMetadata;
    public showAllCachedDeps: boolean;

    public showVersionPickerModal: boolean;
    public versionsToPickFrom: string[] | undefined;
    public selectedVersion?: string | undefined;

    public searchLoadingPromise?: Promise<void>;
    public cacheLoadingPromise?: Promise<void>;

    constructor(
        readonly configStore: ConfigStore,
        @IPackageService readonly packageService: IPackageService,
        @IAppService readonly appService: IAppService,
        @ILogger logger: ILogger
    ) {
        super(logger);
    }

    public attached() {
        Split(["#cached-packages", "#package-search", "#package-info"], {
            gutterSize: 6,
            sizes: [35, 40, 25],
            minSize: [50, 50, 50],
        });

        this.refreshCachedPackages();
        this.searchPackages();
    }

    public async goToPreviousPage() {
        if (this.searchCurrentPage === 1)
            return;

        this.searchCurrentPage--;
    }

    public async goToNextPage() {
        if (this.searchResults?.length < this.searchTake)
            return;

        this.searchCurrentPage++;
    }

    public async selectPackageVersionToInstall(pkg: PackageReference) {
        this.versionsToPickFrom = undefined;
        this.selectedVersion = undefined;
        this.showVersionPickerModal = true;

        this.versionsToPickFrom = (await this.packageService.getPackageVersions(pkg.packageId)).reverse();

        if (!this.versionsToPickFrom || !this.versionsToPickFrom.length) {
            alert("Could not find any versions for package: " + pkg.packageId);
        }
    }

    public async referencePackage(pkg: PackageSearchResult | CachedPackageViewModel, version?: string) {
        this.selectedVersion = undefined;
        this.versionsToPickFrom = undefined;
        this.showVersionPickerModal = false;

        if (!version)
            version = pkg.version;

        if (!version) throw new Error(`Version is null or undefined. Could not reference package.`);

        if (version == pkg.version && pkg.referenced)
            return;

        if (pkg instanceof PackageSearchResult)
            await this.installPackage(pkg, version);

        this.configStore.addReference(new PackageReference({
            packageId: pkg.packageId,
            title: pkg.title,
            version: version
        }));
    }

    public async installPackage(pkg: PackageSearchResult, version?: string) {
        try {
            pkg.isInstalling = true;
            await this.packageService.install(pkg.packageId, version || pkg.version);
            await this.refreshCachedPackages();
        } catch (ex) {
            if (ex instanceof Error) {
                alert(`Download failed. ${ex.message}`);
            }
        } finally {
            pkg.isInstalling = false;
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

    public async purgeCache() {
        if (confirm("Are you sure you want to purge the package cache? This will delete all cached packages.")) {
            await this.packageService.purgePackageCache();
            await this.refreshCachedPackages();
        }
    }

    @watch((vm: PackageManagement) => vm.showAllCachedDeps)
    private async refreshCachedPackages() {
        const promise = this.showAllCachedDeps
            ? this.packageService.getCachedPackages(true)
            : this.packageService.getExplicitlyInstalledCachedPackages(true);

        this.cacheLoadingPromise = promise.then(cps => {
            this.cachedPackages = cps
                .sort((a, b) => (a.title > b.title) ? 1 : ((b.title > a.title) ? -1 : 0))
                .map(p => new CachedPackageViewModel(p));
            this.markReferencedPackages();
        });
    }

    @watch<PackageManagement>(vm => vm.searchTerm)
    @watch<PackageManagement>(vm => vm.searchPrereleases)
    @watch<PackageManagement>(vm => vm.searchTake)
    @watch<PackageManagement>(vm => vm.searchCurrentPage)
    private async searchPackages(term?: string) {
        this.searchLoadingPromise = this.packageService.search(
            term ?? this.searchTerm,
            (this.searchCurrentPage - 1) * this.searchTake,
            this.searchTake,
            this.searchPrereleases)
            .then(data => {
                this.searchResults = data.map(r => new PackageSearchResult(r));
                this.markReferencedPackages();
            });
    }

    @watch<PackageManagement>(vm => vm.configStore.references.length)
    private markReferencedPackages() {
        for (const searchResult of this.searchResults) {
            searchResult.existsInLocalCache = !!this.cachedPackages
                .find(p => p.packageId === searchResult.packageId && p.version === searchResult.version);

            searchResult.referenced = !!this.configStore.references
                .find(r => (r as PackageReference).packageId == searchResult.packageId && (r as PackageReference).version === searchResult.version);
        }

        for (const cachedPackage of this.cachedPackages) {
            cachedPackage.referenced = !!this.configStore.references
                .find(r => (r as PackageReference).packageId == cachedPackage.packageId && (r as PackageReference).version === cachedPackage.version);
        }
    }
}

class PackageSearchResult extends PackageMetadata {
    public existsInLocalCache = false;
    public isInstalling = false;
    public referenced = false;
}

class CachedPackageViewModel extends CachedPackage {
    public referenced = false;
}
