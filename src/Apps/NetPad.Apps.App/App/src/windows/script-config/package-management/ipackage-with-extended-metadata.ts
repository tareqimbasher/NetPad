import {PackageMetadata} from "@domain";

export interface IPackageWithExtendedMetadata extends PackageMetadata {
    isExtMetaLoaded: boolean;
    isExtMetaLoading: boolean;
}
