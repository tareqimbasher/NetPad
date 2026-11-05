import {IPackageService, PackageReference} from "@application";
import {PackageExtendedMetadataLoader} from "./package-extended-metadata-loader";
import {CachedPackageViewModel} from "./package-view-models";

/**
 * The local-cache source containing every cached package version on disk.
 */
export class PackageCache {
    public packages: CachedPackageViewModel[] = [];
    public loading = false;
    public filter = "";
    public showDependencies = false;

    private extLoader?: PackageExtendedMetadataLoader;

    constructor(private readonly packageService: IPackageService) {
    }

    public get visible(): CachedPackageViewModel[] {
        let list = this.packages;

        if (!this.showDependencies) {
            list = list.filter(p => p.installReason !== "Dependency");
        }

        const filter = this.filter.trim().toLowerCase();
        if (filter) {
            list = list.filter(p =>
                p.title?.toLowerCase().includes(filter) || p.packageId?.toLowerCase().includes(filter));
        }

        return list;
    }

    public get visibleIdCount(): number {
        return new Set(this.visible.map(p => p.packageId)).size;
    }

    public cachedVersionsOf(packageId: string): string[] {
        return this.packages
            .filter(p => p.packageId === packageId && p.version)
            .map(p => p.version!);
    }

    public async refresh(): Promise<void> {
        this.loading = true;

        try {
            const cps = await this.packageService.getCachedPackages(false);
            const vms = cps
                .sort((a, b) => (a.title ?? "").localeCompare(b.title ?? "") || (a.version ?? "").localeCompare(b.version ?? ""))
                .map(p => Object.assign(new CachedPackageViewModel(), p));

            this.extLoader?.cancel();
            this.extLoader = new PackageExtendedMetadataLoader(vms, this.packageService);
            this.extLoader.load().finally(() => this.extLoader = undefined);

            this.packages = vms;
        } finally {
            this.loading = false;
        }
    }

    public applyReferences(references: PackageReference[]): void {
        for (const cached of this.packages) {
            cached.referencedVersion = references.find(r => r.packageId === cached.packageId)?.version;
        }
    }

    public dispose(): void {
        this.extLoader?.cancel();
    }
}
