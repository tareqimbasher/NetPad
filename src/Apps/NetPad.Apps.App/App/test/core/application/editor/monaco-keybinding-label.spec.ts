import {parseMonacoKeybindingLabel} from "@application/editor/monaco/monaco-keybinding-label";

describe("parseMonacoKeybindingLabel", () => {
    test("a single stroke is one group of caps", () => {
        expect(parseMonacoKeybindingLabel("Ctrl+Shift+P")).toEqual([["Ctrl", "Shift", "P"]]);
    });

    test("a lone key is one group of one cap", () => {
        expect(parseMonacoKeybindingLabel("F12")).toEqual([["F12"]]);
    });

    test("a two-stroke chord splits on the stroke boundary, not across it", () => {
        expect(parseMonacoKeybindingLabel("Ctrl+K Ctrl+F")).toEqual([["Ctrl", "K"], ["Ctrl", "F"]]);
    });

    test("macOS glyph labels come back as one cap per stroke", () => {
        expect(parseMonacoKeybindingLabel("⇧⌥F")).toEqual([["⇧⌥F"]]);
        expect(parseMonacoKeybindingLabel("⌘K ⌘F")).toEqual([["⌘K"], ["⌘F"]]);
    });
});
