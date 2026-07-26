import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {IPackageService, PackageDependencySet, PackageReference, ViewModelBase} from "@application";
import {Util} from "@common";
import {ConfigStore} from "../../config-store";
import {PackageExtendedMetadataLoader} from "../package-extended-metadata-loader";
import {PackageRowViewModel, PackageSelection} from "../package-view-models";
import {PackageCache} from "../package-cache";
import {computeDetailState, DetailState, emptyDetailState} from "../package-detail-state";

/** The right detail panel. */
export class PackageDetailPanel extends ViewModelBase {
    @bindable public selection?: PackageSelection;
    @bindable public cache: PackageCache;
    @bindable public includePrerelease = false;

    public detail: DetailState = emptyDetailState();

    // Version picker state
    public pickedVersion?: string;
    public fetchedVersions?: string[];
    public versionsLoading = false;
    public showAllVersions = false;
    public versionFilter = "";

    public depsExpanded = false;

    public installing = false;
    public installError?: string;

    private extLoader?: PackageExtendedMetadataLoader;
    private versionsRequest?: AbortController;

    constructor(
        readonly configStore: ConfigStore,
        @IPackageService readonly packageService: IPackageService,
        @ILogger logger: ILogger
    ) {
        super(logger);
    }

    protected override detaching() {
        this.extLoader?.cancel();
        this.versionsRequest?.abort();
        super.detaching();
    }

    private selectionChanged() {
        const pkg = this.selection?.package;

        this.showAllVersions = false;
        this.versionFilter = "";
        this.depsExpanded = false;
        this.installError = undefined;
        this.pickedVersion = this.selection?.version;
        this.fetchedVersions = undefined;

        if (!pkg) {
            this.detail = emptyDetailState();
            return;
        }

        this.ensureExtMeta(pkg);
        this.recompute();
        void this.loadVersions(pkg.packageId);
    }

    private includePrereleaseChanged() {
        const pkg = this.selection?.package;
        if (pkg) void this.loadVersions(pkg.packageId);
    }

    private async loadVersions(packageId: string) {
        this.fetchedVersions = undefined;
        this.versionsLoading = true;

        this.versionsRequest?.abort();
        const request = new AbortController();
        this.versionsRequest = request;

        try {
            const versions = await this.packageService.getPackageVersions(packageId, this.includePrerelease, request.signal);
            if (this.selection?.package?.packageId === packageId) {
                this.fetchedVersions = versions;
                this.recompute();
            }
        } catch (ex) {
            if (request.signal.aborted) return;
            this.logger.error("Could not load package versions", ex);
        } finally {
            if (this.versionsRequest === request && this.selection?.package?.packageId === packageId) {
                this.versionsLoading = false;
            }
        }
    }

    public selectVersion(version: string) {
        this.pickedVersion = version;
        this.installError = undefined;
        this.recompute();
    }

    public expandAllVersions() {
        this.showAllVersions = true;
        this.recompute();
    }

    @watch<PackageDetailPanel>(vm => vm.versionFilter)
    private versionFilterChanged() {
        this.recompute();
    }

    private ensureExtMeta(pkg: PackageRowViewModel) {
        if (pkg.isExtMetaLoaded || pkg.isExtMetaLoading) return;
        this.extLoader?.cancel();
        this.extLoader = new PackageExtendedMetadataLoader([pkg], this.packageService);
        void this.extLoader.load().then(() => {
            if (this.selection?.package === pkg) this.recompute();
        });
    }

    // Derived state

    @watch<PackageDetailPanel>(vm => vm.configStore.references.length)
    @watch<PackageDetailPanel>(vm => vm.cache.packages)
    private recompute() {
        const pkg = this.selection?.package;
        if (!pkg) {
            this.detail = emptyDetailState();
            return;
        }

        this.detail = computeDetailState({
            package: pkg,
            pickedVersion: this.pickedVersion,
            fetchedVersions: this.fetchedVersions,
            expanded: this.showAllVersions,
            filter: this.versionFilter,
            referencedVersion: this.referencedVersionOf(pkg.packageId),
            cachedVersions: this.cache.cachedVersionsOf(pkg.packageId),
        });
    }

    private referencedVersionOf(packageId: string): string | undefined {
        const ref = this.configStore.references
            .find(r => r instanceof PackageReference && r.packageId === packageId) as PackageReference | undefined;
        return ref?.version;
    }

    private get latestVersion(): string | undefined {
        const pkg = this.selection?.package;
        if (!pkg) return undefined;
        return pkg.latestAvailableVersion || pkg.version || this.fetchedVersions?.[0];
    }

    private get targetVersion(): string | undefined {
        return this.pickedVersion || this.latestVersion;
    }

    // Actions

    public async applyPrimary() {
        const action = this.detail.primaryAction;
        if (action) await this.reference(action.version);
    }

    private async reference(version: string) {
        const pkg = this.selection?.package;
        if (!pkg || !version) return;

        try {
            this.installing = true;
            this.installError = undefined;

            if (!this.cache.cachedVersionsOf(pkg.packageId).includes(version)) {
                await this.packageService.install(pkg.packageId, version, this.configStore.script.config.targetFrameworkVersion);
                await this.cache.refresh();
            }

            const existing = this.configStore.references
                .find(r => r instanceof PackageReference && r.packageId === pkg.packageId);
            if (existing) this.configStore.removeReference(existing);

            this.configStore.addReference(new PackageReference({packageId: pkg.packageId, title: pkg.title, version}));
            this.pickedVersion = version;
            this.recompute();
        } catch (ex) {
            this.installError = ex instanceof Error ? ex.message : String(ex);
        } finally {
            this.installing = false;
        }
    }

    public async downloadOnly() {
        const pkg = this.selection?.package;
        const version = this.targetVersion;
        if (!pkg || !version) return;

        this.installing = true;
        this.installError = undefined;

        try {
            await this.packageService.install(pkg.packageId, version, this.configStore.script.config.targetFrameworkVersion);
            await this.cache.refresh();
        } catch (ex) {
            this.installError = ex instanceof Error ? ex.message : String(ex);
        } finally {
            this.installing = false;
        }
    }

    public removeReference() {
        const id = this.selection?.package?.packageId;
        if (!id) return;
        const existing = this.configStore.references
            .find(r => r instanceof PackageReference && r.packageId === id);
        if (existing) this.configStore.removeReference(existing);
        this.recompute();
    }

    public async deleteFromCache() {
        const pkg = this.selection?.package;
        const version = this.targetVersion;
        if (!pkg || !version) return;

        await this.packageService.deleteCachedPackage(pkg.packageId, version);
        await this.cache.refresh();
    }

    // Formatting

    public formatCount(count?: number): string {
        return count == null ? "—" : count.toLocaleString();
    }

    public formatDate(date?: Date): string {
        return date ? Util.formatDate(date, "yyyy-MM-dd") : "—";
    }

    public groupHasDependencies(dep: PackageDependencySet): boolean {
        return !!dep.packages && dep.packages.length > 0;
    }

    // A dependency is formatted "<package id> <range>" (e.g. "Microsoft.CSharp [4.3.0, )").
    // Package ids never contain spaces.
    public dependencyName(dependency: string): string {
        const space = dependency.indexOf(" ");
        return space < 0 ? dependency : dependency.slice(0, space);
    }

    public dependencyRange(dependency: string): string {
        const space = dependency.indexOf(" ");
        return space < 0 ? "" : dependency.slice(space + 1);
    }
}
