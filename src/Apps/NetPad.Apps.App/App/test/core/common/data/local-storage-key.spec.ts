import {migrateLocalStorageKey} from "@common/data/local-storage-key";

describe("migrateLocalStorageKey", () => {
    beforeEach(() => {
        localStorage.clear();
    });

    test("moves the legacy value to the new key", () => {
        localStorage.setItem("legacy", "value");

        migrateLocalStorageKey("legacy", "netpad.new");

        expect(localStorage.getItem("netpad.new")).toBe("value");
        expect(localStorage.getItem("legacy")).toBeNull();
    });

    test("keeps an existing value under the new key and still removes the legacy key", () => {
        localStorage.setItem("legacy", "old");
        localStorage.setItem("netpad.new", "current");

        migrateLocalStorageKey("legacy", "netpad.new");

        expect(localStorage.getItem("netpad.new")).toBe("current");
        expect(localStorage.getItem("legacy")).toBeNull();
    });

    test("does nothing when there is no legacy value", () => {
        localStorage.setItem("netpad.new", "current");

        migrateLocalStorageKey("legacy", "netpad.new");

        expect(localStorage.getItem("netpad.new")).toBe("current");
        expect(localStorage.length).toBe(1);
    });

    test("migrates an empty legacy value", () => {
        localStorage.setItem("legacy", "");

        migrateLocalStorageKey("legacy", "netpad.new");

        expect(localStorage.getItem("netpad.new")).toBe("");
        expect(localStorage.getItem("legacy")).toBeNull();
    });
});
