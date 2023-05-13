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
        ["1.1.1.1", 1, 1, 1, 1],
        ["0.1.1.1", 0, 1, 1, 1],
        ["1.0.1.1", 1, 0, 1, 1],
        ["1.1.0.1", 1, 1, 0, 1],
        ["1.1.1.0", 1, 1, 1, 0],
        ["0.0.0.0", 0, 0, 0, 0],
        ["1.1.1", 1, 1, 1, 0],
        ["0.1.0", 0, 1, 0, 0],
        ["10.1", 10, 1, 0, 0],
        ["10", 10, 0, 0, 0],
        ["25", 25, 0, 0, 0],
        ["23.0.9", 23, 0, 9, 0],
    ])("given string %p, major should be %p, minor should be %p, revision should be %p, build should be %p",
        (versionStr: string, expectedMajor: number, expectedMinor: number, expectedRevision: number, expectedBuild: number) => {
            const version = new Version(versionStr);
            expect(version.major).toEqual(expectedMajor);
            expect(version.minor).toEqual(expectedMinor);
            expect(version.revision).toEqual(expectedRevision);
            expect(version.build).toEqual(expectedBuild);
        }
    );

    test.each([
        [new Version("1.1.1.1"), "1.1.1.1"],
        [new Version("1.1.1.0"), "1.1.1"],
        [new Version("0.1.1.0"), "0.1.1"],
        [new Version("0.0.0"), "0.0.0"],
    ])("calling .toString() on %p should return %p",
        (version: Version, expectedString: string) => {
            expect(version.toString()).toEqual(expectedString);
        }
    );

    test.each([
        [new Version("1.1.1.1"), new Version("1.1.1.1"), true],
        [new Version("1.1.1"), new Version("1.1.1.0"), true],
        [new Version("2.1.1"), new Version("1.1.1"), false],
        [new Version("0.0.0"), new Version("0.0.0"), true],
    ])("given two versions %p and %p, equality should be %p",
        (version1: Version, version2: Version, expectedToBeEqual: boolean) => {
            expect(version1.equals(version2)).toEqual(expectedToBeEqual);
        }
    );

    test.each([
        [new Version("2.0.0.0"), new Version("1.0.0.0")],
        [new Version("1.1.0.0"), new Version("1.0.0.0")],
        [new Version("1.1.1.0"), new Version("1.1.0.0")],
        [new Version("1.1.1.1"), new Version("1.1.1.0")],
        [new Version("1.1.1.2"), new Version("1.1.1.1")],
        [new Version("10.0.1"), new Version("1.0.0.1")],
        [new Version("10.1.0"), new Version("1.0.1")],
        [new Version("10.1.0"), new Version("10.0.1")],
        [new Version("10.0.1"), new Version("1.2.0")],
        [new Version("1.2.1"), new Version("1.2.0")],
        [new Version("3.2.1"), new Version("3.2")],
    ])("version %p should be greater than %p",
        (version1: Version, version2: Version) => {
            expect(version1.greaterThan(version2)).toEqual(true);
        }
    );

    test.each([
        [new Version("1.0.0.0"), new Version("2.0.0.0")],
        [new Version("1.0.0.0"), new Version("1.1.0.0")],
        [new Version("1.1.0.0"), new Version("1.1.1.0")],
        [new Version("1.1.1.0"), new Version("1.1.1.1")],
        [new Version("1.1.1.1"), new Version("1.1.1.2")],
        [new Version("1.0.0.1"), new Version("10.0.1")],
        [new Version("1.0.1"), new Version("10.1.0")],
        [new Version("10.0.1"), new Version("10.1.0")],
        [new Version("1.2.0"), new Version("10.0.1")],
        [new Version("1.2.0"), new Version("1.2.1")],
        [new Version("3.2"), new Version("3.2.1")],
    ])("version %p should be less than %p",
        (version1: Version, version2: Version) => {
            expect(version1.lessThan(version2)).toEqual(true);
        }
    );
});
