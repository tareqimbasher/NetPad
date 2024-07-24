/**
 * Represents a semver version.
 */
export class Version {
    public major = 0;
    public minor = 0;
    public revision = 0;
    public build = 0;

    public get isEmpty(): boolean {
        return this.major + this.minor + this.revision + this.build === 0;
    }

    constructor(versionStr: string) {
        if (!versionStr || typeof versionStr !== "string") return;
        const parts = versionStr.split(".");

        const major = Number(parts[0]);
        if (isNaN(major)) return;
        this.major = major;

        const minor = parts.length >= 2 ? Number(parts[1]) : undefined;
        if (minor !== undefined && !isNaN(minor)) this.minor = minor;

        const revision = parts.length >= 3 ? Number(parts[2]) : undefined;
        if (revision !== undefined && !isNaN(revision)) this.revision = revision;

        const build = parts.length >= 4 ? Number(parts[3]) : undefined;
        if (build !== undefined && !isNaN(build)) this.build = build;
    }

    public greaterThan(other: Version): boolean {
        if (this.equals(other)) return false;

        if (this.major > other.major) return true;
        if (this.major < other.major) return false;

        if (this.minor > other.minor) return true;
        if (this.minor < other.minor) return false;

        if (this.revision > other.revision) return true;
        if (this.revision < other.revision) return false;

        if (this.build > other.build) return true;
        if (this.build < other.build) return false;

        return true;
    }

    public lessThan(other: Version) {
        return !this.greaterThan(other);
    }

    public equals(other: Version) {
        return this.major === other.major
            && this.minor === other.minor
            && this.revision === other.revision
            && this.build === other.build;
    }

    public toString() {
        let str = `${this.major}.${this.minor}.${this.revision}`;
        if (this.build > 0) str += `.${this.build}`;
        return str;
    }
}
