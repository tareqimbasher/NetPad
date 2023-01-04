import {DateTimeValueConverter} from "@application/value-converters/date-time-value-converter";

describe("DateTime Value Converter", () => {
    it("should return null when value is not a date", () => {
        const invalidValues = [1, "text", {}, true];

        const converter = getConverter();

        for (const invalidValue of invalidValues) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const result = converter.toView(invalidValue as any);
            expect(result).toBeNull();
        }
    });

    it("should return a UTC formatted string", () => {
        const converter = getConverter();
        const date = new Date();

        const result = converter.toView(date, "UTC");

        expect(result).toBe(date.toUTCString());
    });

    it("should return a locale formatted string", () => {
        const converter = getConverter();
        const date = new Date();

        const result = converter.toView(date, "Local");

        expect(result).toBe(date.toLocaleString());
    });

    it("should return a UTC formatted string by default", () => {
        const converter = getConverter();
        const date = new Date();

        const result = converter.toView(date);

        expect(result).toBe(date.toUTCString());
    });
});

const getConverter = () => new DateTimeValueConverter();
