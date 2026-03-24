import {Util} from "@common/utils/util";

describe("Util (additional coverage)", () => {
    describe("groupBy", () => {
        test("should group items by key", () => {
            const items = [
                {type: "fruit", name: "apple"},
                {type: "vegetable", name: "carrot"},
                {type: "fruit", name: "banana"},
                {type: "vegetable", name: "broccoli"},
            ];

            const result = Util.groupBy(items, item => item.type);

            expect(result.get("fruit")).toEqual([
                {type: "fruit", name: "apple"},
                {type: "fruit", name: "banana"},
            ]);
            expect(result.get("vegetable")).toEqual([
                {type: "vegetable", name: "carrot"},
                {type: "vegetable", name: "broccoli"},
            ]);
        });

        test("should return empty map for empty array", () => {
            const result = Util.groupBy([], () => "key");
            expect(result.size).toBe(0);
        });

        test("should handle single-item groups", () => {
            const items = [{id: 1}, {id: 2}, {id: 3}];
            const result = Util.groupBy(items, item => item.id);

            expect(result.size).toBe(3);
            expect(result.get(1)).toEqual([{id: 1}]);
        });
    });

    describe("distinct", () => {
        test("should return unique values", () => {
            expect(Util.distinct([1, 2, 2, 3, 3, 3])).toEqual([1, 2, 3]);
        });

        test("should return empty array for empty input", () => {
            expect(Util.distinct([])).toEqual([]);
        });

        test("should preserve order of first occurrences", () => {
            expect(Util.distinct([3, 1, 2, 1, 3])).toEqual([3, 1, 2]);
        });

        test("should work with strings", () => {
            expect(Util.distinct(["a", "b", "a", "c"])).toEqual(["a", "b", "c"]);
        });

        test("should not deduplicate objects by value", () => {
            const a = {id: 1};
            const b = {id: 1};
            expect(Util.distinct([a, b])).toEqual([a, b]); // different references
        });

        test("should deduplicate same object references", () => {
            const obj = {id: 1};
            expect(Util.distinct([obj, obj, obj])).toEqual([obj]);
        });
    });

    describe("debounce", () => {
        beforeEach(() => {
            jest.useFakeTimers();
        });

        afterEach(() => {
            jest.useRealTimers();
        });

        test("should delay execution", () => {
            const fn = jest.fn();
            const debounced = Util.debounce(null, fn, 100);

            debounced();

            expect(fn).not.toHaveBeenCalled();

            jest.advanceTimersByTime(100);

            expect(fn).toHaveBeenCalledTimes(1);
        });

        test("should reset timer on subsequent calls", () => {
            const fn = jest.fn();
            const debounced = Util.debounce(null, fn, 100);

            debounced();
            jest.advanceTimersByTime(50);
            debounced(); // reset
            jest.advanceTimersByTime(50);

            expect(fn).not.toHaveBeenCalled();

            jest.advanceTimersByTime(50);
            expect(fn).toHaveBeenCalledTimes(1);
        });

        test("should pass arguments to the function", () => {
            const fn = jest.fn();
            const debounced = Util.debounce(null, fn, 100);

            debounced("a", "b");

            jest.advanceTimersByTime(100);

            expect(fn).toHaveBeenCalledWith("a", "b");
        });

        test("should use the correct thisArg", () => {
            let captured: unknown;
            const fn = function(this: unknown) { captured = this; };
            const context = {name: "myContext"};
            const debounced = Util.debounce(context, fn, 100);

            debounced();
            jest.advanceTimersByTime(100);

            expect(captured).toBe(context);
        });

        test("with immediate=true should execute immediately on first call", () => {
            const fn = jest.fn();
            const debounced = Util.debounce(null, fn, 100, true);

            debounced();

            expect(fn).toHaveBeenCalledTimes(1);
        });

        test("with immediate=true should not execute again during wait period", () => {
            const fn = jest.fn();
            const debounced = Util.debounce(null, fn, 100, true);

            debounced(); // immediate call
            expect(fn).toHaveBeenCalledTimes(1);

            debounced(); // during wait, no immediate call
            debounced(); // during wait, no immediate call
            expect(fn).toHaveBeenCalledTimes(1);

            // When the timeout fires, the trailing call also executes
            // because isImmediateCall was false for the later calls
            jest.advanceTimersByTime(100);
            expect(fn).toHaveBeenCalledTimes(2);
        });
    });

    describe("formatDurationMs", () => {
        test.each([
            [0, "0ms"],
            [-1, "0ms"],
            [500, "500ms"],
            [1000, "1s"],
            [1234, "1s 234ms"],
            [60000, "1m"],
            [61000, "1m 1s"],
            [3600000, "1h"],
            [3661234, "1h 1m 1s 234ms"],
            [86400000, "1d"],
            [90061234, "1d 1h 1m 1s 234ms"],
        ])("formatDurationMs(%i) should return '%s'",
            (ms, expected) => {
                expect(Util.formatDurationMs(ms)).toBe(expected);
            });
    });

    describe("formatString", () => {
        test("should replace positional placeholders", () => {
            expect(Util.formatString("{0} {1}!", "Hello", "World")).toBe("Hello World!");
        });

        test("should leave unmatched placeholders unchanged", () => {
            expect(Util.formatString("{0} {1} {2}", "A", "B")).toBe("A B {2}");
        });

        test("should handle null args as empty string", () => {
            expect(Util.formatString("{0}", null)).toBe("");
        });

        test("should leave placeholder when arg is undefined", () => {
            // undefined means "not provided", so the placeholder is left as-is
            expect(Util.formatString("{0}", undefined)).toBe("{0}");
        });

        test("should handle repeated placeholders", () => {
            expect(Util.formatString("{0} and {0}", "X")).toBe("X and X");
        });
    });

    describe("trimStart", () => {
        test.each([
            ["///path", "/", "path"],
            ["   hello", " ", "hello"],
            ["hello", "x", "hello"],
            ["xxhello", "x", "hello"],
        ])("trimStart(%p, %p) should return %p",
            (str, char, expected) => {
                expect(Util.trimStart(str, char)).toBe(expected);
            });

        test("should return empty-like strings as-is", () => {
            expect(Util.trimStart("", "/")).toBe("");
        });
    });

    describe("trimEnd", () => {
        test.each([
            ["path///", "/", "path"],
            ["hello   ", " ", "hello"],
            ["hello", "x", "hello"],
            ["helloxx", "x", "hello"],
        ])("trimEnd(%p, %p) should return %p",
            (str, char, expected) => {
                expect(Util.trimEnd(str, char)).toBe(expected);
            });

        test("should return empty-like strings as-is", () => {
            expect(Util.trimEnd("", "/")).toBe("");
        });
    });
});
