import {Version} from "@common/data/version";

describe("Version", () => {
    test.each([3, "d.", "text", new Date(), {}])("given invalid value %p, version should be empty",
        invalidValue => {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const version = new Version(invalidValue as any);
            expect(version.isEmpty).toEqual(true);
        }
    );

    test.each([
        ["1.1.1", 1, 1, 1, undefined],
        ["0.1.1", 0, 1, 1, undefined],
        ["1.0.1", 1, 0, 1, undefined],
        ["1.1.0", 1, 1, 0, undefined],
        ["0.0.0", 0, 0, 0, undefined],
        ["0.1.0", 0, 1, 0, undefined],
        ["10.1", 10, 1, 0, undefined],
        ["10", 10, 0, 0, undefined],
        ["25", 25, 0, 0, undefined],
        ["23.0.9", 23, 0, 9, undefined],
        ["1.0.0-alpha", 1, 0, 0, "alpha"],
        ["1.0.0-alpha.1", 1, 0, 0, "alpha.1"],
        ["0.12.0-beta.1", 0, 12, 0, "beta.1"],
        ["0.12.0-beta.2", 0, 12, 0, "beta.2"],
        ["1.0.0-rc.1", 1, 0, 0, "rc.1"],
        ["2.0.0-preview.3", 2, 0, 0, "preview.3"],
        ["1.0.0-beta-rc.1", 1, 0, 0, "beta-rc.1"],
    ])("given string %p, major should be %p, minor should be %p, patch should be %p, preReleaseLabel should be %p",
        (versionStr: string, expectedMajor: number, expectedMinor: number, expectedPatch: number, expectedPreReleaseLabel: string | undefined) => {
            const version = new Version(versionStr);
            expect(version.major).toEqual(expectedMajor);
            expect(version.minor).toEqual(expectedMinor);
            expect(version.patch).toEqual(expectedPatch);
            expect(version.preReleaseLabel).toEqual(expectedPreReleaseLabel);
        }
    );

    test("build metadata is stripped and ignored", () => {
        const version = new Version("1.2.3-beta.1+build123");
        expect(version.major).toEqual(1);
        expect(version.minor).toEqual(2);
        expect(version.patch).toEqual(3);
        expect(version.preReleaseLabel).toEqual("beta.1");
    });

    test("build metadata without pre-release label is stripped", () => {
        const version = new Version("1.2.3+build456");
        expect(version.major).toEqual(1);
        expect(version.minor).toEqual(2);
        expect(version.patch).toEqual(3);
        expect(version.preReleaseLabel).toBeUndefined();
    });

    test.each([
        [new Version("1.1.1"), "1.1.1"],
        [new Version("0.1.1"), "0.1.1"],
        [new Version("0.0.0"), "0.0.0"],
        [new Version("1.0.0-alpha"), "1.0.0-alpha"],
        [new Version("0.12.0-beta.1"), "0.12.0-beta.1"],
        [new Version("1.0.0-rc.1"), "1.0.0-rc.1"],
        [new Version("1.2.3-beta.1+build123"), "1.2.3-beta.1"],
    ])("calling .toString() on %p should return %p",
        (version: Version, expectedString: string) => {
            expect(version.toString()).toEqual(expectedString);
        }
    );

    test.each([
        [new Version("1.1.1"), new Version("1.1.1"), true],
        [new Version("0.0.0"), new Version("0.0.0"), true],
        [new Version("2.1.1"), new Version("1.1.1"), false],
        [new Version("1.0.0-alpha"), new Version("1.0.0-alpha"), true],
        [new Version("1.0.0-beta.1"), new Version("1.0.0-beta.1"), true],
        [new Version("1.0.0-alpha"), new Version("1.0.0-beta"), false],
        [new Version("1.0.0-alpha"), new Version("1.0.0"), false],
    ])("given two versions %p and %p, equality should be %p",
        (version1: Version, version2: Version, expectedToBeEqual: boolean) => {
            expect(version1.equals(version2)).toEqual(expectedToBeEqual);
        }
    );

    test.each([
        [new Version("2.0.0"), new Version("1.0.0")],
        [new Version("1.1.0"), new Version("1.0.0")],
        [new Version("1.1.1"), new Version("1.1.0")],
        [new Version("10.0.1"), new Version("1.0.0")],
        [new Version("10.1.0"), new Version("1.0.1")],
        [new Version("10.1.0"), new Version("10.0.1")],
        [new Version("10.0.1"), new Version("1.2.0")],
        [new Version("1.2.1"), new Version("1.2.0")],
        [new Version("3.2.1"), new Version("3.2.0")],
        // Pre-release: stable > pre-release with same version
        [new Version("1.0.0"), new Version("1.0.0-alpha")],
        [new Version("1.0.0"), new Version("1.0.0-beta.1")],
        [new Version("0.12.0"), new Version("0.12.0-beta.1")],
        // Pre-release ordering
        [new Version("1.0.0-beta"), new Version("1.0.0-alpha")],
        [new Version("1.0.0-rc"), new Version("1.0.0-beta")],
        [new Version("1.0.0-beta.2"), new Version("1.0.0-beta.1")],
        [new Version("0.12.0-beta.2"), new Version("0.12.0-beta.1")],
        [new Version("0.12.0-beta.10"), new Version("0.12.0-beta.9")],
        // Pre-release with more identifiers has higher precedence
        [new Version("1.0.0-beta.1"), new Version("1.0.0-beta")],
        // Higher version pre-release > lower version stable
        [new Version("0.13.0-beta.1"), new Version("0.12.0")],
        [new Version("1.0.0-alpha"), new Version("0.99.99")],
    ])("version %p should be greater than %p",
        (version1: Version, version2: Version) => {
            expect(version1.greaterThan(version2)).toEqual(true);
        }
    );

    test.each([
        [new Version("1.0.0"), new Version("2.0.0")],
        [new Version("1.0.0"), new Version("1.1.0")],
        [new Version("1.1.0"), new Version("1.1.1")],
        [new Version("1.0.0"), new Version("10.0.1")],
        [new Version("1.0.1"), new Version("10.1.0")],
        [new Version("10.0.1"), new Version("10.1.0")],
        [new Version("1.2.0"), new Version("10.0.1")],
        [new Version("1.2.0"), new Version("1.2.1")],
        [new Version("3.2.0"), new Version("3.2.1")],
        // Pre-release < stable with same version
        [new Version("1.0.0-alpha"), new Version("1.0.0")],
        [new Version("1.0.0-beta.1"), new Version("1.0.0")],
        [new Version("0.12.0-beta.1"), new Version("0.12.0")],
        // Pre-release ordering
        [new Version("1.0.0-alpha"), new Version("1.0.0-beta")],
        [new Version("1.0.0-beta"), new Version("1.0.0-rc")],
        [new Version("1.0.0-beta.1"), new Version("1.0.0-beta.2")],
        [new Version("0.12.0-beta.1"), new Version("0.12.0-beta.2")],
        [new Version("0.12.0-beta.9"), new Version("0.12.0-beta.10")],
    ])("version %p should be less than %p",
        (version1: Version, version2: Version) => {
            expect(version1.lessThan(version2)).toEqual(true);
        }
    );

    test("equal versions are not greater than or less than each other", () => {
        const v1 = new Version("1.0.0");
        const v2 = new Version("1.0.0");
        expect(v1.greaterThan(v2)).toEqual(false);
        expect(v1.lessThan(v2)).toEqual(false);
        expect(v1.equals(v2)).toEqual(true);
    });

    test("equal pre-release versions are not greater than or less than each other", () => {
        const v1 = new Version("1.0.0-beta.1");
        const v2 = new Version("1.0.0-beta.1");
        expect(v1.greaterThan(v2)).toEqual(false);
        expect(v1.lessThan(v2)).toEqual(false);
        expect(v1.equals(v2)).toEqual(true);
    });

    test("isEmpty returns true for 0.0.0", () => {
        expect(new Version("0.0.0").isEmpty).toEqual(true);
    });

    test("isEmpty returns false for pre-release of 0.0.0", () => {
        expect(new Version("0.0.0-alpha").isEmpty).toEqual(false);
    });

    test("isEmpty returns false for non-zero version", () => {
        expect(new Version("0.1.0").isEmpty).toEqual(false);
    });
});
