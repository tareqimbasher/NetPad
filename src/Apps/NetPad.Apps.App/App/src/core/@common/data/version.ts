/**
 * Represents a semantic version (semver 2.0).
 * Supports pre-release labels like "0.12.0-beta.1".
 */
export class Version {
    public major = 0;
    public minor = 0;
    public patch = 0;
    public preReleaseLabel: string | undefined;

    public get isEmpty(): boolean {
        return this.major + this.minor + this.patch === 0 && !this.preReleaseLabel;
    }

    constructor(versionStr: string) {
        if (!versionStr || typeof versionStr !== "string") return;

        // Strip build metadata (+...) — ignored per semver 2.0
        const plusIndex = versionStr.indexOf("+");
        if (plusIndex !== -1) {
            versionStr = versionStr.substring(0, plusIndex);
        }

        // Split pre-release label from version core at the first '-'
        const dashIndex = versionStr.indexOf("-");
        let versionCore: string;
        if (dashIndex !== -1) {
            versionCore = versionStr.substring(0, dashIndex);
            const label = versionStr.substring(dashIndex + 1);
            if (label.length > 0) {
                this.preReleaseLabel = label;
            }
        } else {
            versionCore = versionStr;
        }

        const parts = versionCore.split(".");

        const major = Number(parts[0]);
        if (isNaN(major)) return;
        this.major = major;

        const minor = parts.length >= 2 ? Number(parts[1]) : undefined;
        if (minor !== undefined && !isNaN(minor)) this.minor = minor;

        const patch = parts.length >= 3 ? Number(parts[2]) : undefined;
        if (patch !== undefined && !isNaN(patch)) this.patch = patch;
    }

    public greaterThan(other: Version): boolean {
        if (this.equals(other)) return false;

        if (this.major > other.major) return true;
        if (this.major < other.major) return false;

        if (this.minor > other.minor) return true;
        if (this.minor < other.minor) return false;

        if (this.patch > other.patch) return true;
        if (this.patch < other.patch) return false;

        return Version.comparePreRelease(this.preReleaseLabel, other.preReleaseLabel) > 0;
    }

    public lessThan(other: Version): boolean {
        return !this.greaterThan(other) && !this.equals(other);
    }

    public equals(other: Version): boolean {
        return this.major === other.major
            && this.minor === other.minor
            && this.patch === other.patch
            && this.preReleaseLabel === other.preReleaseLabel;
    }

    public toString(): string {
        let str = `${this.major}.${this.minor}.${this.patch}`;
        if (this.preReleaseLabel) str += `-${this.preReleaseLabel}`;
        return str;
    }

    /**
     * Compares two pre-release labels per semver 2.0 rules.
     * Returns >0 if a has higher precedence, <0 if b does, 0 if equal.
     *
     * Rules:
     * - No label (stable) > has label (pre-release)
     * - Dot-separated identifiers compared left to right
     * - Numeric identifiers compared numerically
     * - Alphanumeric identifiers compared lexically (ASCII)
     * - Numeric identifiers have lower precedence than alphanumeric
     * - Larger set of identifiers has higher precedence if all preceding are equal
     */
    private static comparePreRelease(a: string | undefined, b: string | undefined): number {
        if (a === b) return 0;

        // No label (stable release) has higher precedence than a pre-release label
        if (!a && b) return 1;
        if (a && !b) return -1;

        const partsA = a!.split(".");
        const partsB = b!.split(".");
        const len = Math.min(partsA.length, partsB.length);

        for (let i = 0; i < len; i++) {
            const idA = partsA[i];
            const idB = partsB[i];

            if (idA === idB) continue;

            const numA = /^\d+$/.test(idA) ? Number(idA) : undefined;
            const numB = /^\d+$/.test(idB) ? Number(idB) : undefined;

            // Both numeric: compare numerically
            if (numA !== undefined && numB !== undefined) {
                return numA - numB;
            }

            // Numeric has lower precedence than alphanumeric
            if (numA !== undefined) return -1;
            if (numB !== undefined) return 1;

            // Both alphanumeric: compare lexically
            return idA < idB ? -1 : 1;
        }

        // All preceding identifiers equal — longer set has higher precedence
        return partsA.length - partsB.length;
    }
}
