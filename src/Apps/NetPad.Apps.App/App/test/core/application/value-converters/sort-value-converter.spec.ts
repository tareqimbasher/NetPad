import {SortValueConverter} from "@application/value-converters/sort-value-converter";

describe("SortValueConverter", () => {
    const converter = new SortValueConverter();

    test("should return empty array for non-array input", () => {
        expect(converter.toView(null as never, "name")).toEqual([]);
        expect(converter.toView(undefined as never, "name")).toEqual([]);
        expect(converter.toView("string" as never, "name")).toEqual([]);
    });

    test("should return same array for empty array", () => {
        const arr: unknown[] = [];
        expect(converter.toView(arr, "name")).toBe(arr);
    });

    describe("ordinal comparison (default)", () => {
        test("should sort strings ascending by property", () => {
            const items = [{name: "Charlie"}, {name: "Alice"}, {name: "Bob"}];
            const result = converter.toView(items, "name", "asc", "ordinal");

            expect(result.map(i => i.name)).toEqual(["Alice", "Bob", "Charlie"]);
        });

        test("should sort strings descending by property", () => {
            const items = [{name: "Charlie"}, {name: "Alice"}, {name: "Bob"}];
            const result = converter.toView(items, "name", "desc", "ordinal");

            expect(result.map(i => i.name)).toEqual(["Charlie", "Bob", "Alice"]);
        });

        test("should handle null/undefined values by sorting them first", () => {
            const items = [{name: "Bob"}, {name: null}, {name: "Alice"}];
            const result = converter.toView(items, "name", "asc", "ordinal");

            expect(result.map(i => i.name)).toEqual([null, "Alice", "Bob"]);
        });
    });

    describe("ordinal comparison — both null", () => {
        test("should treat two null values as equal", () => {
            const items = [{name: null}, {name: null}, {name: "Alice"}];
            const result = converter.toView(items, "name", "asc", "ordinal");

            // Both nulls sort before "Alice", their relative order is stable (return 0)
            expect(result.map(i => i.name)).toEqual([null, null, "Alice"]);
        });
    });

    describe("ordinalIgnoreCase comparison", () => {
        test("should sort case-insensitively", () => {
            const items = [{name: "charlie"}, {name: "Alice"}, {name: "BOB"}];
            const result = converter.toView(items, "name", "asc", "ordinalIgnoreCase");

            expect(result.map(i => i.name)).toEqual(["Alice", "BOB", "charlie"]);
        });
    });

    describe("number comparison", () => {
        test("should sort numbers ascending", () => {
            const items = [{val: 30}, {val: 10}, {val: 20}];
            const result = converter.toView(items, "val", "asc", "number");

            expect(result.map(i => i.val)).toEqual([10, 20, 30]);
        });

        test("should sort numbers descending", () => {
            const items = [{val: 30}, {val: 10}, {val: 20}];
            const result = converter.toView(items, "val", "desc", "number");

            expect(result.map(i => i.val)).toEqual([30, 20, 10]);
        });

        test("should handle null values", () => {
            const items = [{val: 10}, {val: null}, {val: 5}];
            const result = converter.toView(items, "val", "asc", "number");

            expect(result.map(i => i.val)).toEqual([null, 5, 10]);
        });
    });

    describe("numeral comparison", () => {
        test("should sort string numbers as numeric values", () => {
            const items = [{val: "30"}, {val: "5"}, {val: "10"}];
            const result = converter.toView(items, "val", "asc", "numeral");

            expect(result.map(i => i.val)).toEqual(["5", "10", "30"]);
        });

        test("should handle NaN values by sorting them first", () => {
            const items = [{val: "10"}, {val: "abc"}, {val: "5"}];
            const result = converter.toView(items, "val", "asc", "numeral");

            expect(result.map(i => i.val)).toEqual(["abc", "5", "10"]);
        });
    });

    describe("date comparison", () => {
        test("should sort dates ascending", () => {
            const items = [
                {date: "2023-03-15"},
                {date: "2023-01-01"},
                {date: "2023-06-30"},
            ];
            const result = converter.toView(items, "date", "asc", "date");

            expect(result.map(i => i.date)).toEqual(["2023-01-01", "2023-03-15", "2023-06-30"]);
        });

        test("should sort dates descending", () => {
            const items = [
                {date: "2023-03-15"},
                {date: "2023-01-01"},
                {date: "2023-06-30"},
            ];
            const result = converter.toView(items, "date", "desc", "date");

            expect(result.map(i => i.date)).toEqual(["2023-06-30", "2023-03-15", "2023-01-01"]);
        });
    });

    describe("without propertyName", () => {
        test("should sort primitive values directly", () => {
            const items = ["Charlie", "Alice", "Bob"];
            const result = converter.toView(items, "", "asc", "ordinal");

            expect(result).toEqual(["Alice", "Bob", "Charlie"]);
        });

        test("should sort numbers directly", () => {
            const items = [30, 10, 20];
            const result = converter.toView(items, "", "asc", "number");

            expect(result).toEqual([10, 20, 30]);
        });
    });

    test("should return array unchanged for unknown comparison type", () => {
        const items = [{name: "B"}, {name: "A"}];
        const result = converter.toView(items, "name", "asc", "unknown" as never);

        expect(result).toBe(items);
    });
});
