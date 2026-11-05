import {Settings} from "@application/api";
import {serializationLimit, SettingsDraft} from "../../../src/windows/settings/settings-draft";

const saved = (results: Record<string, unknown> = {}, mode = "Dark") => Settings.fromJS({
    appearance: {mode, themeFamily: "steel"},
    results: {openOnRun: true, textWrap: false, maxSerializationDepth: 64, maxCollectionSerializeLength: 1000, ...results},
});

describe("serializationLimit", () => {
    test.each(["", "0", "-5", "1.5", "abc", null, undefined])("sends %p as 0, the backend's default", value => {
        expect(serializationLimit(value)).toBe(0);
    });

    test("sends a positive whole number as it is", () => {
        expect(serializationLimit("128")).toBe(128);
        expect(serializationLimit(128)).toBe(128);
    });
});

describe("SettingsDraft", () => {
    test("sends an empty limit as 0 and leaves the edited field as typed", () => {
        const draft = new SettingsDraft(saved());
        draft.edited.results.maxSerializationDepth = "" as unknown as number;

        const toSave = draft.beginSave();

        expect(toSave.results.maxSerializationDepth).toBe(0);
        expect(draft.edited.results.maxSerializationDepth).toBe("");
    });

    test("a field the save sent takes the value the backend stored", () => {
        const draft = new SettingsDraft(saved());
        draft.edited.results.maxSerializationDepth = "" as unknown as number;
        draft.edited.results.maxCollectionSerializeLength = "50000" as unknown as number;

        draft.beginSave();
        draft.syncSaved(saved({maxSerializationDepth: 64, maxCollectionSerializeLength: 10000}));

        expect(draft.edited.results.maxSerializationDepth).toBe(64);
        expect(draft.edited.results.maxCollectionSerializeLength).toBe(10000);
    });

    test("a field edited while the save is in flight keeps the edit", () => {
        const draft = new SettingsDraft(saved());
        draft.edited.results.maxSerializationDepth = "" as unknown as number;

        draft.beginSave();
        draft.edited.appearance.mode = "Light";
        draft.syncSaved(saved());

        expect(draft.edited.appearance.mode).toBe("Light");
        expect(draft.edited.results.maxSerializationDepth).toBe(64);
    });

    test("after a failed save, settings saved elsewhere don't replace the edits", () => {
        const draft = new SettingsDraft(saved());
        draft.edited.results.maxSerializationDepth = "128" as unknown as number;

        draft.beginSave();
        draft.saveFailed();
        draft.syncSaved(saved({}, "Light"));

        expect(draft.edited.results.maxSerializationDepth).toBe("128");
        expect(draft.edited.appearance.mode).toBe("Light");
    });

    test("settings saved elsewhere replace values the user hasn't changed", () => {
        const draft = new SettingsDraft(saved());
        draft.edited.results.textWrap = true;

        draft.syncSaved(saved({openOnRun: false}));

        expect(draft.edited.results.openOnRun).toBe(false);
        expect(draft.edited.results.textWrap).toBe(true);
    });
});
