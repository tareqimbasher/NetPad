import {LocalStorageValue} from "@common/data/local-storage-value";

describe("LocalStorageValue", () => {
    beforeEach(() => {
        localStorage.clear();
    });

    test("should return undefined when no value in localStorage", () => {
        const lsv = new LocalStorageValue<string>("test-key");
        expect(lsv.value).toBeUndefined();
    });

    test("should read and parse value from localStorage", () => {
        localStorage.setItem("test-key", JSON.stringify("hello"));
        const lsv = new LocalStorageValue<string>("test-key");

        expect(lsv.value).toBe("hello");
    });

    test("should read and parse object values from localStorage", () => {
        const obj = {name: "test", count: 42};
        localStorage.setItem("test-key", JSON.stringify(obj));
        const lsv = new LocalStorageValue<{name: string; count: number}>("test-key");

        expect(lsv.value).toEqual(obj);
    });

    test("should cache value after first read", () => {
        localStorage.setItem("test-key", JSON.stringify("original"));
        const lsv = new LocalStorageValue<string>("test-key");

        expect(lsv.value).toBe("original");

        // Change localStorage directly - cached value should be returned
        localStorage.setItem("test-key", JSON.stringify("changed"));
        expect(lsv.value).toBe("original");
    });

    test("should return undefined for invalid JSON", () => {
        localStorage.setItem("test-key", "not valid json {{{");
        const lsv = new LocalStorageValue<string>("test-key");

        expect(lsv.value).toBeUndefined();
    });

    test("set value should update the in-memory value", () => {
        const lsv = new LocalStorageValue<string>("test-key");
        lsv.value = "new-value";

        expect(lsv.value).toBe("new-value");
    });

    describe("save", () => {
        test("should persist value to localStorage", () => {
            const lsv = new LocalStorageValue<string>("test-key");
            lsv.value = "saved-value";
            lsv.save();

            expect(localStorage.getItem("test-key")).toBe(JSON.stringify("saved-value"));
        });

        test("should persist object values to localStorage", () => {
            const obj = {name: "test", count: 42};
            const lsv = new LocalStorageValue<{name: string; count: number}>("test-key");
            lsv.value = obj;
            lsv.save();

            expect(JSON.parse(localStorage.getItem("test-key")!)).toEqual(obj);
        });

        test("should remove from localStorage when value is undefined", () => {
            localStorage.setItem("test-key", JSON.stringify("existing"));
            const lsv = new LocalStorageValue<string>("test-key");
            lsv.value = undefined;
            lsv.save();

            expect(localStorage.getItem("test-key")).toBeNull();
        });
    });

    test("should re-read localStorage after invalid JSON is corrected", () => {
        // When JSON parse fails, _value is set to undefined, which means the
        // next read will hit localStorage again (no stale cache)
        localStorage.setItem("test-key", "not valid json");
        const lsv = new LocalStorageValue<string>("test-key");

        expect(lsv.value).toBeUndefined();

        // Fix the value in localStorage
        localStorage.setItem("test-key", JSON.stringify("fixed"));
        expect(lsv.value).toBe("fixed");
    });

    test("should expose storageKey", () => {
        const lsv = new LocalStorageValue<string>("my-key");
        expect(lsv.storageKey).toBe("my-key");
    });
});
