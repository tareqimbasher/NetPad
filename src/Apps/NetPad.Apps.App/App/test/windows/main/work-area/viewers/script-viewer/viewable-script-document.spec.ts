import {ScriptKind} from "../../../../../../src/core/@application/api";
import {
    ViewableScriptDocument,
} from "../../../../../../src/windows/main/work-area/viewers/script-viewer/viewable-script-document";

describe("ViewableScriptDocument.tryGetLanguageFromScriptKind", () => {
    test("maps Program to csharp", () => {
        expect(ViewableScriptDocument.tryGetLanguageFromScriptKind("Program")).toBe("csharp");
    });

    test("maps Expression to csharp", () => {
        // "Expression" is the default value of the ScriptKind enum, so any script config without an
        // explicit kind deserializes into it. It must not be left unmapped.
        expect(ViewableScriptDocument.tryGetLanguageFromScriptKind("Expression")).toBe("csharp");
    });

    test("maps SQL to sql", () => {
        expect(ViewableScriptDocument.tryGetLanguageFromScriptKind("SQL")).toBe("sql");
    });

    test("returns undefined for an unknown kind", () => {
        expect(
            ViewableScriptDocument.tryGetLanguageFromScriptKind("NotAKind" as ScriptKind),
        ).toBeUndefined();
    });
});
