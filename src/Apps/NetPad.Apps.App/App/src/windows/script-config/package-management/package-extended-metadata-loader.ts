import {IPackageService, PackageIdentity} from "@application";
import {IPackageWithExtendedMetadata} from "./ipackage-with-extended-metadata";

export class PackageExtendedMetadataLoader {
    private abortController: AbortController | undefined;

    constructor(private readonly packages: IPackageWithExtendedMetadata[],
                private readonly packageService: IPackageService) {
    }

    public async load(): Promise<void> {
        this.abortController = new AbortController();

        const packages = [...this.packages.filter(p => !p.isExtMetaLoading && !p.isExtMetaLoaded && p.version)];
        packages.forEach(p => p.isExtMetaLoading = true);

        try {
            const chunkSize = 10;

            for (let i = 0; i < packages.length; i += chunkSize) {
                if (this.abortController.signal.aborted) {
                    break;
                }

                const batch = packages.slice(i, i + chunkSize);

                await this.do(batch, this.abortController.signal);
            }
        } catch (ex) {
            console.error(ex);
        } finally {
            packages.forEach(p => p.isExtMetaLoading = false);
        }
    }

    public cancel() {
        this.abortController?.abort();
    }

    private async do(packages: IPackageWithExtendedMetadata[], abortSignal: AbortSignal) {
        const metadatas = await this.packageService
            .getPackageMetadata(packages.map(p => {
                return new PackageIdentity({
                    id: p.packageId,
                    version: p.version!
                })
            }), abortSignal);

        if (metadatas.length === 0) {
            return;
        }

        for (const metadata of metadatas) {
            const pkg = packages.find(p => p.packageId == metadata.packageId && p.version == metadata.version);
            if (!pkg) continue;

            // Create an init object because "pkg" could be a derived type which has its own init
            // implementation. If "metadata" does not include props in the derived type (pkg)
            // then the init() call will wipe those prop values.
            const initObj = Object.assign(pkg.clone(), metadata);
            pkg.init(initObj);
        }
    }
}
