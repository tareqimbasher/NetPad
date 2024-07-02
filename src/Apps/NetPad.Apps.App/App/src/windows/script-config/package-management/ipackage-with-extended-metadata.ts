import {PackageMetadata} from "@application";

export interface IPackageWithExtendedMetadata extends PackageMetadata {
    isExtMetaLoaded: boolean;
    isExtMetaLoading: boolean;
}
