import {SemanticVersion} from "../api";

declare module "../api" {
    interface SemanticVersion {
        greaterThan(other: SemanticVersion): boolean;
        lessThan(other: SemanticVersion): boolean;
        equals(other: SemanticVersion): boolean;
        toString(): string;
    }
}

SemanticVersion.prototype.equals = function (other: SemanticVersion): boolean {
    return this.major === other.major
        && this.minor === other.minor
        && this.patch === other.patch
        && this.preReleaseLabel === other.preReleaseLabel
        && this.buildLabel === other.buildLabel;
}

SemanticVersion.prototype.greaterThan = function (other: SemanticVersion): boolean {
    if (this.equals(other)) {
        return false;
    }

    if (this.major > other.major
        || this.minor > other.minor
        || this.patch > other.patch) {
        return true;
    }

    if (!this.preReleaseLabel && other.preReleaseLabel) {
        return true;
    }

    if (this.preReleaseLabel && !other.preReleaseLabel) {
        return false;
    }

    if (!this.buildLabel && other.buildLabel) {
        return false;
    }

    if (this.buildLabel && !other.buildLabel) {
        return true;
    }

    return false;
}

SemanticVersion.prototype.lessThan = function (other: SemanticVersion): boolean {
    return !this.equals(other) && !this.greaterThan(other);
}

SemanticVersion.prototype.toString = function (this: SemanticVersion) {
    let str = `${this.major}.${this.minor}.${this.patch}`;

    if (this.preReleaseLabel) {
        str += `-${this.preReleaseLabel}`;
    }

    if (this.buildLabel) {
        str += `+${this.buildLabel}`;
    }

    return str;
}
