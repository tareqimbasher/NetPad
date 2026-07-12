import {TimeValueConverter} from "@application/value-converters/time-value-converter";

describe("Time Value Converter", () => {
    it("should return null when value is not a date", () => {
        const invalidValues = [1, "text", {}, true];

        const converter = getConverter();

        for (const invalidValue of invalidValues) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const result = converter.toView(invalidValue as any);
            expect(result).toBeNull();
        }
    });

    it("should return locale formatted time", () => {
        const converter = getConverter();
        const date = new Date("2020-01-01T08:01:30.000Z");

        const result = converter.toView(date);
        expect(result).toBe(date.toLocaleTimeString([], {hour: "numeric", minute: "2-digit"}));
    });
});

const getConverter = () => new TimeValueConverter();
