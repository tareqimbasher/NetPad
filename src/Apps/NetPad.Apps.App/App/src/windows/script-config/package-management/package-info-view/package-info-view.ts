import {IPackageService} from "@application";
import {bindable} from "aurelia";
import {PackageExtendedMetadataLoader} from "../package-extended-metadata-loader";
import {IPackageWithExtendedMetadata} from "../ipackage-with-extended-metadata";

export class PackageInfoView {
    @bindable package?: IPackageWithExtendedMetadata;
    private extMetaLoader?: PackageExtendedMetadataLoader;

    constructor(@IPackageService private readonly packageService: IPackageService) {
    }

    packageChanged() {
        if (!this.package || this.package.isExtMetaLoaded || this.package.isExtMetaLoading) return;

        if (this.extMetaLoader) {
            this.extMetaLoader.cancel();
        }

        this.extMetaLoader = new PackageExtendedMetadataLoader([this.package], this.packageService);
        this.extMetaLoader.load();
    }
}

