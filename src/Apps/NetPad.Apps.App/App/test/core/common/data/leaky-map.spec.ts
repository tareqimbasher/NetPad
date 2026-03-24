import {LeakyMap} from "@common/data/leaky-map";

describe("LeakyMap", () => {
    beforeEach(() => {
        jest.useFakeTimers();
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    test("should store and retrieve values like a regular Map", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);
        map.set("b", 2);

        expect(map.get("a")).toBe(1);
        expect(map.get("b")).toBe(2);
        expect(map.size).toBe(2);
    });

    test("should auto-delete entries after timeout", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);

        jest.advanceTimersByTime(999);
        expect(map.has("a")).toBe(true);

        jest.advanceTimersByTime(1);
        expect(map.has("a")).toBe(false);
    });

    test("get() should reset the timeout", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);

        jest.advanceTimersByTime(800);
        map.get("a"); // reset timer

        jest.advanceTimersByTime(800);
        expect(map.has("a")).toBe(true); // only 800ms since last access

        jest.advanceTimersByTime(200);
        expect(map.has("a")).toBe(false); // now 1000ms since last access
    });

    test("set() should reset the timeout", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);

        jest.advanceTimersByTime(800);
        map.set("a", 2); // reset timer at t=800

        jest.advanceTimersByTime(999);
        expect(map.has("a")).toBe(true); // 999ms since reset, still alive

        jest.advanceTimersByTime(1);
        expect(map.has("a")).toBe(false); // 1000ms since reset, evicted
    });

    test("delete() should clear the timeout", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);

        map.delete("a");

        expect(map.has("a")).toBe(false);
        // Advancing timers should not cause errors
        jest.advanceTimersByTime(2000);
    });

    test("clear() should clear all timeouts", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);
        map.set("b", 2);
        map.set("c", 3);

        map.clear();

        expect(map.size).toBe(0);
        // Advancing timers should not cause errors
        jest.advanceTimersByTime(2000);
    });

    test("different keys should have independent timeouts", () => {
        const map = new LeakyMap<string, number>(1000);
        map.set("a", 1);

        jest.advanceTimersByTime(500);
        map.set("b", 2);

        jest.advanceTimersByTime(500);
        // "a" was set 1000ms ago, should be gone
        expect(map.has("a")).toBe(false);
        // "b" was set 500ms ago, should still be here
        expect(map.has("b")).toBe(true);

        jest.advanceTimersByTime(500);
        expect(map.has("b")).toBe(false);
    });

    describe("constructor validation", () => {
        test("should throw for negative timeout", () => {
            expect(() => new LeakyMap(-1)).toThrow(TypeError);
        });

        test("should throw for Infinity timeout", () => {
            expect(() => new LeakyMap(Infinity)).toThrow(TypeError);
        });

        test("should throw for NaN timeout", () => {
            expect(() => new LeakyMap(NaN)).toThrow(TypeError);
        });

        test("should allow zero timeout", () => {
            expect(() => new LeakyMap(0)).not.toThrow();
        });
    });
});
