import {CachedPackage, PackageMetadata} from "@application/api";

export interface IPackageWithExtendedMetadata extends PackageMetadata {
    isExtMetaLoaded: boolean;
    isExtMetaLoading: boolean;
}

/** A package row in the list. */
export interface PackageRowViewModel extends IPackageWithExtendedMetadata {
    referencedVersion?: string;
    readonly initials: string;
    readonly showReferencedChip: boolean;
    readonly showDependencyChip: boolean;
    readonly hasUpdate: boolean;

    /** Called when the icon URL fails to load an icon. */
    onIconError(): void;
}

/** The panel's current selection. */
export interface PackageSelection {
    package: PackageRowViewModel;
    version?: string;
}

/** Extracts a package's initials from its title. */
export function packageInitials(title?: string): string {
    if (!title) {
        return "?";
    }

    const capitalsAndNums = title.match(/[A-Z0-9]/g) ?? [];
    let initials = capitalsAndNums.slice(0, 2).join("");
    if (initials.length < 2) {
        initials = title.replace(/[^a-zA-Z0-9]/g, "").slice(0, 2);
    }

    return initials.toUpperCase() || "?";
}

function hasNewerVersion(pkg: PackageMetadata): boolean {
    return !!pkg.latestAvailableVersion && pkg.latestAvailableVersion !== pkg.version;
}

export class SearchResultViewModel extends PackageMetadata implements PackageRowViewModel {
    public referencedVersion?: string;
    public isExtMetaLoading = false;

    public get isExtMetaLoaded(): boolean {
        return !!this.publishedDate;
    }

    public get initials(): string {
        return packageInitials(this.title);
    }

    // A feed row shows "referenced" whenever the package is referenced at any version. The row lists
    // the latest, which may differ from the referenced version.
    public get showReferencedChip(): boolean {
        return !!this.referencedVersion;
    }

    public get showDependencyChip(): boolean {
        return false;
    }

    public get hasUpdate(): boolean {
        return hasNewerVersion(this);
    }

    public onIconError(): void {
        this.iconUrl = undefined;
    }
}

export class CachedPackageViewModel extends CachedPackage implements PackageRowViewModel {
    public referencedVersion?: string;
    public isExtMetaLoading = false;

    public get isExtMetaLoaded(): boolean {
        return !!this.publishedDate;
    }

    public get initials(): string {
        return packageInitials(this.title);
    }

    // A cache row is a specific version, so it shows "referenced" only when that exact version is
    // the one referenced.
    public get showReferencedChip(): boolean {
        return this.version === this.referencedVersion;
    }

    public get showDependencyChip(): boolean {
        return this.installReason === "Dependency";
    }

    public get hasUpdate(): boolean {
        return hasNewerVersion(this);
    }

    public onIconError(): void {
        this.iconUrl = undefined;
    }
}
