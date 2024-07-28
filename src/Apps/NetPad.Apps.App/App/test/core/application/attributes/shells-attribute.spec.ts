import {ShellsCustomAttribute} from "@application/attributes/shells-attribute";

describe("ShellsCustomAttribute.areRequirementsMet()", () => {
    it("should return true when requirements is undefined", () => {
        const remove = ShellsCustomAttribute.areRequirementsMet(undefined, "browser");
        expect(remove).toBe(true);
    });

    it("should return true when requirements is empty", () => {
        const remove = ShellsCustomAttribute.areRequirementsMet("", "browser");
        expect(remove).toBe(true);
    });

    test.each(<[string, "browser" | "electron"][]>[
        ["browser", "browser"],
        ["electron", "electron"],
        ["electron,browser", "browser"],
        ["electron,browser", "electron"],
        ["!browser", "electron"],
        ["!electron", "browser"],
    ])("should return true when requirements is %s and shell is %s",
        (requirements: string, currentShell: "browser" | "electron") => {
            const requirementsMet = ShellsCustomAttribute.areRequirementsMet(requirements, currentShell);
            expect(requirementsMet).toBe(true);
        });

    test.each(<[string, "browser" | "electron"][]>[
        ["browser", "electron"],
        ["electron", "browser"],
        ["!browser", "browser"],
        ["!electron", "electron"],
    ])("should return false when requirements is %s and shell is %s",
        (requirements: string, currentShell: "browser" | "electron") => {
            const requirementsMet = ShellsCustomAttribute.areRequirementsMet(requirements, currentShell);
            expect(requirementsMet).toBe(false);
        });
});
