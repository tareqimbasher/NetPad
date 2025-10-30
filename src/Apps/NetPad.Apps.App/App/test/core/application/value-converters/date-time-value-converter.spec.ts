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

    it("should return locale formatted date", () => {
        const converter = getConverter();
        const date = new Date("2020-01-01T08:01:30.000Z");

        const result = converter.toView(date);
        expect(result).toBe(date.toLocaleString());
    });
});

const getConverter = () => new DateTimeValueConverter();
