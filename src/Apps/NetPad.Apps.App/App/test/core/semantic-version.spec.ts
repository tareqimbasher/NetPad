import {SemanticVersion} from "@domain";

describe("Version", () => {
    test.each([
        ["1.1.1.1", "1.1.1.1"],
        ["1.1.1.0", "1.1.1"],
        ["0.1.1.0", "0.1.1"],
        ["0.0.0.0", "0.0.0"],
    ])("calling .toString() on %p should return %p",
        (versionStr: string, expectedString: string) => {
            const v = new SemanticVersion({
                major: 1,
                minor: 1,
                patch: 1,
                string: versionStr
            })

            expect(v.toString()).toEqual(expectedString);
        }
    );

    // test.each([
    //     [new SemanticVersion("1.1.1.1"), new SemanticVersion("1.1.1.1"), true],
    //     [new SemanticVersion("1.1.1"), new SemanticVersion("1.1.1.0"), true],
    //     [new SemanticVersion("2.1.1"), new SemanticVersion("1.1.1"), false],
    //     [new SemanticVersion("0.0.0"), new SemanticVersion("0.0.0"), true],
    // ])("given two versions %p and %p, equality should be %p",
    //     (version1: SemanticVersion, version2: SemanticVersion, expectedToBeEqual: boolean) => {
    //         expect(version1.equals(version2)).toEqual(expectedToBeEqual);
    //     }
    // );
    //
    // test.each([
    //     [new SemanticVersion("2.0.0.0"), new SemanticVersion("1.0.0.0")],
    //     [new SemanticVersion("1.1.0.0"), new SemanticVersion("1.0.0.0")],
    //     [new SemanticVersion("1.1.1.0"), new SemanticVersion("1.1.0.0")],
    //     [new SemanticVersion("1.1.1.1"), new SemanticVersion("1.1.1.0")],
    //     [new SemanticVersion("1.1.1.2"), new SemanticVersion("1.1.1.1")],
    //     [new SemanticVersion("10.0.1"), new SemanticVersion("1.0.0.1")],
    //     [new SemanticVersion("10.1.0"), new SemanticVersion("1.0.1")],
    //     [new SemanticVersion("10.1.0"), new SemanticVersion("10.0.1")],
    //     [new SemanticVersion("10.0.1"), new SemanticVersion("1.2.0")],
    //     [new SemanticVersion("1.2.1"), new SemanticVersion("1.2.0")],
    //     [new SemanticVersion("3.2.1"), new SemanticVersion("3.2")],
    // ])("version %p should be greater than %p",
    //     (version1: SemanticVersion, version2: SemanticVersion) => {
    //         expect(version1.greaterThan(version2)).toEqual(true);
    //     }
    // );
    //
    // test.each([
    //     [new SemanticVersion("1.0.0.0"), new SemanticVersion("2.0.0.0")],
    //     [new SemanticVersion("1.0.0.0"), new SemanticVersion("1.1.0.0")],
    //     [new SemanticVersion("1.1.0.0"), new SemanticVersion("1.1.1.0")],
    //     [new SemanticVersion("1.1.1.0"), new SemanticVersion("1.1.1.1")],
    //     [new SemanticVersion("1.1.1.1"), new SemanticVersion("1.1.1.2")],
    //     [new SemanticVersion("1.0.0.1"), new SemanticVersion("10.0.1")],
    //     [new SemanticVersion("1.0.1"), new SemanticVersion("10.1.0")],
    //     [new SemanticVersion("10.0.1"), new SemanticVersion("10.1.0")],
    //     [new SemanticVersion("1.2.0"), new SemanticVersion("10.0.1")],
    //     [new SemanticVersion("1.2.0"), new SemanticVersion("1.2.1")],
    //     [new SemanticVersion("3.2"), new SemanticVersion("3.2.1")],
    // ])("version %p should be less than %p",
    //     (version1: SemanticVersion, version2: SemanticVersion) => {
    //         expect(version1.lessThan(version2)).toEqual(true);
    //     }
    // );
});
