import {YesNoValueConverter} from "@application/value-converters/yes-no-value-converter";

describe ("Yes/No Value Converter", () => {
    it("should return null when it cannot convert value", () => {
        const invalidValues = [3, "text", new Date(), {}];
        const converter = new YesNoValueConverter();

        for (const invalidValue of invalidValues) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const result = converter.toView(invalidValue as any);
            expect(result).toBeNull();
        }
    });

   it("should return Yes when given a true boolean value", () => {
       const converter = new YesNoValueConverter();
       const result = converter.toView(true);
       expect(result).toBe("Yes");
    });

    it("should return Yes when given a true string value", () => {
        const converter = new YesNoValueConverter();
        const result = converter.toView("true");
        expect(result).toBe("Yes");
    });

    it("should return Yes when given a true string value regardless of casing", () => {
        const converter = new YesNoValueConverter();
        const result = converter.toView("tRuE");
        expect(result).toBe("Yes");
    });

    it("should return No when given a false boolean value", () => {
        const converter = new YesNoValueConverter();
        const result = converter.toView(false);
        expect(result).toBe("No");
    });

    it("should return No when given a false string value", () => {
        const converter = new YesNoValueConverter();
        const result = converter.toView("false");
        expect(result).toBe("No");
    });

    it("should return No when given a false string value regardless of casing", () => {
        const converter = new YesNoValueConverter();
        const result = converter.toView("fAlSe");
        expect(result).toBe("No");
    });
});
