import {IPackageService, PackageIdentity} from "@application";
import {IPackageWithExtendedMetadata} from "./package-view-models";

export class PackageExtendedMetadataLoader {
    private abortController: AbortController | undefined;

    constructor(private readonly packages: IPackageWithExtendedMetadata[],
                private readonly packageService: IPackageService) {
    }

    public async load(): Promise<void> {
        this.cancel();
        const abortController = this.abortController = new AbortController();

        const packages = [...this.packages.filter(p => !p.isExtMetaLoading && !p.isExtMetaLoaded && p.version)];
        packages.forEach(p => p.isExtMetaLoading = true);

        try {
            const chunkSize = 10;

            for (let i = 0; i < packages.length; i += chunkSize) {
                if (abortController.signal.aborted) {
                    break;
                }

                const batch = packages.slice(i, i + chunkSize);

                await this.doLoad(batch, abortController.signal);
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

    private async doLoad(packages: IPackageWithExtendedMetadata[], abortSignal: AbortSignal) {
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
            if (!pkg) {
                continue;
            }

            // Merge onto a clone so a derived type's own props (which the base init() doesn't touch)
            // survive, and only set fields the extended metadata actually carries
            const patch: Record<string, unknown> = {};
            for (const [key, value] of Object.entries(metadata)) {
                if (value !== null && value !== undefined) {
                    patch[key] = value;
                }
            }

            const initObj = Object.assign(pkg.clone(), patch);
            pkg.init(initObj);
        }
    }
}
