import {TakeValueConverter} from "@application/value-converters/take-value-converter";

describe("Take Value Converter", () => {
    it("should return an empty array if passed an invalid value", () => {
        const invalidValues = [true, {}, new Date(), "text", 0];

        const converter = getConverter();

        for (const invalidValue of invalidValues) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const result = converter.toView(invalidValue as any, 1);
            expect(result).toStrictEqual([]);
        }
    });

    it("should return an empty array if passed null or undefined", () => {
        const converter = getConverter();
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const result = converter.toView(null as any, 1);
        expect(result).toStrictEqual([]);
    });

    it("should return an empty array if take param is passed as null", () => {
        const converter = getConverter();
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const result = converter.toView([1, 2], null as any);
        expect(result).toStrictEqual([]);
    });

    it("should return an empty array if take param is passed as undefined", () => {
        const converter = getConverter();
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const result = converter.toView([1, 2], undefined as any);
        expect(result).toStrictEqual([]);
    });

    it("should slice an array correctly", () => {
        const converter = getConverter();
        const result = converter.toView([1, 2, 3, 4], 2);
        expect(result).toStrictEqual([1, 2]);
    });

    it("should return the same array instance if array is empty", () => {
        const array: unknown[] = [];
        const converter = getConverter();
        const result = converter.toView(array, 1);
        expect(result).toBe(array);
    });

    it("should return the same array instance if take is greater than the length of the array", () => {
        const array = [1, 2, 3];
        const converter = getConverter();
        const result = converter.toView(array, array.length + 1);
        expect(result).toBe(array);
    });
});

const getConverter = () => new TakeValueConverter();
