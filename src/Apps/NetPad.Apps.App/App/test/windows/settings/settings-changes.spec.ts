import {Settings} from "@application/api";
import {collectChanges, mergeSavedSettings} from "../../../src/windows/settings/settings-changes";

const changes = (saved: unknown, edited: unknown) => {
    const changed = new Set<string>();
    collectChanges(saved, edited, "", changed);
    return [...changed];
};

describe("mergeSavedSettings", () => {
    test("takes a saved value for a leaf the user did not change", () => {
        const base = {appearance: {mode: "Dark", themeFamily: "steel"}};
        const edited = {appearance: {mode: "Dark", themeFamily: "steel"}};
        const saved = {appearance: {mode: "Light", themeFamily: "steel"}};

        mergeSavedSettings(base, edited, saved);

        expect(edited.appearance.mode).toBe("Light");
        expect(changes(saved, edited)).toEqual([]);
    });

    test("keeps the user's value for a leaf the user changed", () => {
        const base = {appearance: {mode: "Dark", themeFamily: "steel"}};
        const edited = {appearance: {mode: "Dark", themeFamily: "inkwell"}};
        const saved = {appearance: {mode: "Light", themeFamily: "steel"}};

        mergeSavedSettings(base, edited, saved);

        expect(edited.appearance).toEqual({mode: "Light", themeFamily: "inkwell"});
        expect(changes(saved, edited)).toEqual(["appearance.themeFamily"]);
    });

    test("keeps the user's value when the saved value changed too", () => {
        const base = {appearance: {mode: "Dark"}};
        const edited = {appearance: {mode: "System"}};
        const saved = {appearance: {mode: "Light"}};

        mergeSavedSettings(base, edited, saved);

        expect(edited.appearance.mode).toBe("System");
    });

    test("treats a collection as one leaf", () => {
        const base = {editor: {namespaces: ["System"]}, results: {list: ["a"]}};
        const edited = {editor: {namespaces: ["System", "System.Linq"]}, results: {list: ["a"]}};
        const saved = {editor: {namespaces: ["System", "System.Text"]}, results: {list: ["a", "b"]}};

        mergeSavedSettings(base, edited, saved);

        expect(edited.editor.namespaces).toEqual(["System", "System.Linq"]);
        expect(edited.results.list).toEqual(["a", "b"]);
    });

    test("updates the objects in place, so bindings to them stay attached", () => {
        const base = {appearance: {mode: "Dark"}};
        const edited = {appearance: {mode: "Dark"}};
        const appearance = edited.appearance;

        mergeSavedSettings(base, edited, {appearance: {mode: "Light"}});

        expect(edited.appearance).toBe(appearance);
        expect(appearance.mode).toBe("Light");
    });

    test("works on settings objects", () => {
        const data = {appearance: {mode: "Dark", themeFamily: "steel", background: "Neutral"}};
        const base = Settings.fromJS(data);
        const edited = Settings.fromJS(data);
        edited.appearance.themeFamily = "inkwell";
        const saved = Settings.fromJS({appearance: {...data.appearance, mode: "Light"}});

        mergeSavedSettings(base, edited as unknown as Record<string, unknown>, saved as unknown as Record<string, unknown>);

        expect(edited.appearance.mode).toBe("Light");
        expect(edited.appearance.themeFamily).toBe("inkwell");
        expect(changes(saved, edited)).toEqual(["appearance.themeFamily"]);
    });
});
