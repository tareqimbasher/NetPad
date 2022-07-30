import {TextToHtmlValueConverter} from "@application/value-converters/text-to-html-value-converter";

describe("Text to HTML Value Converter", () => {
    it("should return null when passed an invalid value", () => {
        const invalidValues = [null, undefined, 1, true, {}, new Date()];
        const converter = getConverter();

        for (const invalidValue of invalidValues) {
            const result = converter.toView(invalidValue as any);
            expect(result).toBeNull();
        }
    });

    it("should replace spaces with HTML spaces", () => {
        const input = "word word  word   word";
        const converter = getConverter();
        const result = converter.toView(input);
        expect(result).toBe("word&nbsp;word&nbsp;&nbsp;word&nbsp;&nbsp;&nbsp;word");
    });

    it("should replace new lines with HTML breaks", () => {
        const input = "line1\nline2\nline3";
        const converter = getConverter();
        const result = converter.toView(input);
        expect(result).toBe("line1<br/>line2<br/>line3");
    });

    it("should replace < with HTML less than", () => {
        const input = "word<word<";
        const converter = getConverter();
        const result = converter.toView(input);
        expect(result).toBe("word&lt;word&lt;");
    });

    it("should replace > with HTML greater than", () => {
        const input = "word>word>";
        const converter = getConverter();
        const result = converter.toView(input);
        expect(result).toBe("word&gt;word&gt;");
    });
});

const getConverter = () => new TextToHtmlValueConverter();
