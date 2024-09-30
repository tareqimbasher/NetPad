import {ShellsCustomAttribute} from "@application/attributes/shells-attribute";
import {ShellType} from "@application/windows/shell-type";

describe("ShellsCustomAttribute.areRequirementsMet()", () => {
    it("should return true when requirements is undefined", () => {
        const remove = ShellsCustomAttribute.areRequirementsMet(undefined, ShellType.Browser);
        expect(remove).toBe(true);
    });

    it("should return true when requirements is empty", () => {
        const remove = ShellsCustomAttribute.areRequirementsMet("", ShellType.Browser);
        expect(remove).toBe(true);
    });

    test.each([
        ["browser", ShellType.Browser],
        ["electron", ShellType.Electron],
        ["electron,browser", ShellType.Browser],
        ["electron,browser", ShellType.Electron],
        ["electron,browser,tauri", ShellType.Browser],
        ["electron,browser,tauri", ShellType.Electron],
        ["electron,browser,tauri", ShellType.Tauri],
        ["!browser", ShellType.Electron],
        ["!browser", ShellType.Tauri],
        ["!electron", ShellType.Browser],
        ["!electron", ShellType.Tauri],
        ["!tauri", ShellType.Browser],
        ["!tauri", ShellType.Electron],
        ["browser,!tauri", ShellType.Browser],
    ])("should return true when requirements is %s and shell is %s",
        (requirements: string, currentShell: ShellType) => {
            const requirementsMet = ShellsCustomAttribute.areRequirementsMet(requirements, currentShell);
            expect(requirementsMet).toBe(true);
        });

    test.each([
        ["browser", ShellType.Electron],
        ["electron", ShellType.Browser],
        ["tauri", ShellType.Browser],
        ["electron", ShellType.Tauri],
        ["!browser", ShellType.Browser],
        ["!electron", ShellType.Electron],
        ["!tauri", ShellType.Tauri],
        ["browser,!electron", ShellType.Electron],
        ["browser,!tauri", ShellType.Electron],
    ])("should return false when requirements is %s and shell is %s",
        (requirements: string, currentShell: ShellType) => {
            const requirementsMet = ShellsCustomAttribute.areRequirementsMet(requirements, currentShell);
            expect(requirementsMet).toBe(false);
        });
});
