import {Util} from "@common/utils/util";

describe("Util", () => {
    describe("newGuid", () => {
        test("should return a valid GUID", () => {
            const guid = Util.newGuid();

            expect(new RegExp('^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$', 'i')
                .test(guid))
                .toBe(true);
        });
    });

    describe("dateToFormattedString", () => {
        test.each([
            [new Date("2020-03-15 4:35:13"), "yyyy", "2020"],
            [new Date("2020-03-15 4:35:13"), "MM", "03"],
            [new Date("2020-11-15 4:35:13"), "MM", "11"],
            [new Date("2020-03-15 4:35:13"), "M", "3"],
            [new Date("2020-11-15 4:35:13"), "M", "11"],
            [new Date("2020-03-07 4:35:13"), "dd", "07"],
            [new Date("2020-03-15 4:35:13"), "dd", "15"],
            [new Date("2020-03-07 4:35:13"), "d", "7"],
            [new Date("2020-03-15 4:35:13"), "d", "15"],
            [new Date("2020-03-15 4:35:13"), "HH", "04"],
            [new Date("2020-03-15 16:35:13"), "HH", "16"],
            [new Date("2020-03-15 4:35:13"), "H", "4"],
            [new Date("2020-03-15 16:35:13"), "H", "16"],
            [new Date("2020-03-15 4:03:13"), "mm", "03"],
            [new Date("2020-03-15 4:35:13"), "mm", "35"],
            [new Date("2020-03-15 4:03:13"), "m", "3"],
            [new Date("2020-03-15 4:35:13"), "m", "35"],
            [new Date("2020-03-15 4:03:13"), "ss", "13"],
            [new Date("2020-03-15 4:35:03"), "ss", "03"],
            [new Date("2020-03-15 4:03:13"), "s", "13"],
            [new Date("2020-03-15 4:35:03"), "s", "3"],
            [new Date("2020-03-15 4:35:03:123"), "fff", "123"],
            [new Date("2020-03-15 4:35:03:12"), "fff", "012"],
            [new Date("2020-03-15 4:35:03:1"), "fff", "001"],
            [new Date("2020-03-15 4:35:03:123"), "ff", "12"],
            [new Date("2020-03-15 4:35:03:12"), "ff", "12"],
            [new Date("2020-03-15 4:35:03:1"), "ff", "01"],
            [new Date("2020-03-15 4:35:03:123"), "f", "1"],
            [new Date("2020-03-15 4:35:03:12"), "f", "1"],
            [new Date("2020-03-15 4:35:03:1"), "f", "1"],
        ])("given date %p and format %p, should return %p",
            (date, format, expectedFormattedDateString) => {
                expect(Util.dateToFormattedString(date, format)).toBe(expectedFormattedDateString);
            });
    });

    describe("dateDiffInDays", () => {
        test.each([
            [new Date("2020-03-15 12:00"), new Date("2020-03-16 12:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2020-03-14 12:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2020-03-16 3:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2020-03-16 15:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2020-03-14 3:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2020-03-14 15:00"), 1],
            [new Date("2020-03-15 12:00"), new Date("2021-03-15 4:00"), 365],
        ])("given 2 dates %p and %p, should return a diff of %p day(s)",
            (date1, date2, expectedDiffDays) => {
                expect(Util.dateDiffInDays(date1, date2)).toBe(expectedDiffDays);
            });
    });

    describe("toTitleCase", () => {
        test.each([
            ["The fox jumped", "The Fox Jumped"],
            ["the fox jumped", "The Fox Jumped"],
            ["THE FOX JUMPED", "The Fox Jumped"],
            ["thE fOX jUMPED", "The Fox Jumped"],
        ])("given %p, should return %p",
            (str, expectedResult) => {
                expect(Util.toTitleCase(str)).toBe(expectedResult);
            });
    });

    describe("truncate", () => {
        test.each([
            ["The fox jumped", -1, "The fox jumped"],
            ["The fox jumped", 0, "The fox jumped"],
            ["The fox jumped", 1, "T..."],
            ["The fox jumped", 2, "Th..."],
            ["The fox jumped", 3, "The..."],
            ["The fox jumped", 4, "The ..."],
            ["The fox jumped", 5, "The f..."],
            ["The fox jumped", 100, "The fox jumped"],
            [55, 1, 55],
            [true, 1, true],
        ])("given %p and a max length of %p, should return %p",
            (str, maxLength, expectedResult) => {
                expect(Util.truncate(str as string, maxLength)).toBe(expectedResult);
            });
    });

    describe("trim", () => {
        test.each([
            [" The fox", " ", "The fox"],
            ["The fox ", " ", "The fox"],
            [" The fox ", " ", "The fox"],
            [" The fox ", "x", " The fox "],
            [" The fox", "x", " The fo"],
            ["xThe fox", "x", "The fo"],
            ["xThe foxxx", "x", "The fo"],
        ])("should trim %p remove %p and return %p",
            (str, trimCharacter, expectedResult) => {
                expect(Util.trim(str, trimCharacter)).toBe(expectedResult);
            });
    });

    describe("trimAny", () => {
        test.each([
            [" The fox", [" "], "The fox"],
            ["The fox ", [" "], "The fox"],
            [" The fox ", [" "], "The fox"],
            [" The fox ", ["x"], " The fox "],
            [" The fox", ["x"], " The fo"],
            ["xThe fox", ["x"], "The fo"],
            ["xThe foxxx", ["x"], "The fo"],
            ["xThe foxxx", ["x", "o"], "The f"],
            ["xThe foxxx", ["x", "f", "o"], "The "],
            ["xThe foxxx", ["x", "o", "f"], "The "],
            ["xThe foxxx", ["x", "f", "o", " "], "The"],
        ])("should trim %p remove %p and return %p",
            (str, trimCharacters, expectedResult) => {
                expect(Util.trimAny(str, ...trimCharacters)).toBe(expectedResult);
            });
    });

    describe("trimWord", () => {
        test.each([
            ["The fox", "fox", "The "],
            ["The fox ", "fox", "The fox "],
            ["The fox ", "The", " fox "],
            ["The fox ", "The ", "fox "],
            ["The fox", "The fox", ""],
        ])("should trim %p remove %p and return %p",
            (str, trimWord, expectedResult) => {
                expect(Util.trimWord(str, trimWord)).toBe(expectedResult);
            });
    });

    describe("isLetter", () => {
        test.each([
            ["a", true],
            ["g", true],
            ["A", true],
            ["2", false],
            [".", false],
            ["/", false],
            ["gg", false],
        ])("given %p, should return %p",
            (letter, expectedIsLetter) => {
                expect(Util.isLetter(letter)).toBe(expectedIsLetter);
            });
    });
});
