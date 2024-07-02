import Split from "split.js";
import {
    CachedPackage,
    IAppService,
    IPackageService,
    PackageMetadata,
    PackageReference,
    ViewModelBase
} from "@application";
import {ConfigStore} from "../config-store";
import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {PackageExtendedMetadataLoader} from "./package-extended-metadata-loader";
import {IPackageWithExtendedMetadata} from "./ipackage-with-extended-metadata";

export class PackageManagement extends ViewModelBase {
    public searchTerm: string;
    public searchTake = 15;
    public searchPrereleases = false;
    public searchCurrentPage = 1;
    public searchLoadingPromise?: Promise<void>;
    public searchResults: PackageSearchResult[] = [];

    public cachedPackages: CachedPackageViewModel[] = [];
    public cacheLoadingPromise?: Promise<void>;
    public showAllCachedDeps: boolean;
    private cachedPackagedExtendedMetadataLoader: PackageExtendedMetadataLoader | undefined;

    public selectedPackage?: PackageMetadata;
    public showDescriptions = false;
    public showVersionPickerModal: boolean;
    public versionsToPickFrom: string[] | undefined;
    public selectedVersion?: string | undefined;

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

        this.versionsToPickFrom = (await this.packageService.getPackageVersions(pkg.packageId, this.searchPrereleases)).reverse();

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
            await this.packageService.install(pkg.packageId, version || pkg.version, this.configStore.script.config.targetFrameworkVersion);
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
            ? this.packageService.getCachedPackages(false)
            : this.packageService.getExplicitlyInstalledCachedPackages(false);

        this.cacheLoadingPromise = promise.then(cps => {
            const cachedPackages = cps
                .sort((a, b) => (a.title > b.title) ? 1 : ((b.title > a.title) ? -1 : 0))
                .map(p => new CachedPackageViewModel(p));

            if (this.cachedPackagedExtendedMetadataLoader) {
                this.cachedPackagedExtendedMetadataLoader.cancel();
            }

            this.cachedPackagedExtendedMetadataLoader = new PackageExtendedMetadataLoader(cachedPackages, this.packageService);
            this.cachedPackagedExtendedMetadataLoader.load()
                .finally(() => this.cachedPackagedExtendedMetadataLoader = undefined);

            this.cachedPackages = cachedPackages;
            this.markReferencedPackages();
        });
    }

    @watch<PackageManagement>(vm => vm.searchTerm)
    private async searchTermChanged() {
        // Users would expect to go back to page 1 for new results
        this.searchCurrentPage = 1;
        await this.searchPackages();
    }

    @watch<PackageManagement>(vm => vm.searchPrereleases)
    @watch<PackageManagement>(vm => vm.searchTake)
    @watch<PackageManagement>(vm => vm.searchCurrentPage)
    private async searchPackages() {
        this.searchLoadingPromise = this.packageService.search(
            this.searchTerm,
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

class PackageSearchResult extends PackageMetadata implements IPackageWithExtendedMetadata {
    public existsInLocalCache = false;
    public isInstalling = false;
    public referenced = false;

    public get isExtMetaLoaded(): boolean {
        return !!this.publishedDate;
    }

    public isExtMetaLoading: boolean;
}

class CachedPackageViewModel extends CachedPackage implements IPackageWithExtendedMetadata {
    public referenced = false;

    public get isExtMetaLoaded(): boolean {
        return !!this.publishedDate;
    }

    public isExtMetaLoading: boolean;
}

