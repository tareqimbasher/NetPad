import {LangLogoValueConverter} from "@application/value-converters/lang-logo-value-converter";

describe("LangLogoValueConverter", () => {
    const converter = new LangLogoValueConverter();

    test.each([
        ["Program", "img/csharp-logo.png"],
        ["Expression", "img/csharp-logo.png"],
        ["SQL", "img/sql-logo.svg"],
    ])("given ScriptKind '%s', should return '%s'",
        (scriptKind, expectedLogo) => {
            expect(converter.toView(scriptKind as never)).toBe(expectedLogo);
        });

    test.each([
        [null],
        [undefined],
        [""],
        ["Unknown"],
    ])("given invalid value %p, should return null",
        (value) => {
            expect(converter.toView(value as never)).toBeNull();
        });
});
