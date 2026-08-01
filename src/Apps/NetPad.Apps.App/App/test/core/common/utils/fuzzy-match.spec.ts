import {fuzzyMatch, toMatchSegments} from "@common/utils/fuzzy-match";

function positions(text: string, query: string): number[] | undefined {
    return fuzzyMatch(text, query)?.positions;
}

function score(text: string, query: string): number {
    const match = fuzzyMatch(text, query);
    if (!match) throw new Error(`"${query}" did not match "${text}"`);
    return match.score;
}

describe("fuzzyMatch", () => {
    test("an empty query matches anything", () => {
        expect(fuzzyMatch("Open File", "")).toEqual({score: 0, positions: []});
    });

    test("a query that is not a subsequence does not match", () => {
        expect(fuzzyMatch("Open File", "xyz")).toBeUndefined();
        expect(fuzzyMatch("Open File", "eo")).toBeUndefined();
        expect(fuzzyMatch("", "a")).toBeUndefined();
    });

    test("matching ignores case", () => {
        expect(positions("Open File", "OF")).toEqual([0, 5]);
        expect(positions("Open File", "of")).toEqual([0, 5]);
    });

    test("characters may be spread across the string", () => {
        expect(positions("Switch to Last Active Script", "op")).toEqual([8, 26]);
    });

    test("a later occurrence is taken when it starts a word", () => {
        expect(positions("Toggle Full Screen", "tfs")).toEqual([0, 7, 12]);
    });

    test("a run that continues the previous character is kept over a later word start", () => {
        expect(positions("Save As", "sa")).toEqual([0, 1]);
    });

    test("initials of words beat a scattered match", () => {
        expect(score("Go to Script", "gts")).toBeGreaterThan(score("Toggle Developer Tools", "gts"));
    });

    test("a prefix match beats the same characters found later", () => {
        expect(score("Open File", "op")).toBeGreaterThan(score("Script Properties", "op"));
    });

    test("consecutive characters beat the same count spread out", () => {
        // Same length, so only the tightness of the match separates them.
        expect(score("Stop Script", "st")).toBeGreaterThan(score("Save Script", "st"));
    });

    test("the shorter of two equally matched strings wins", () => {
        expect(score("Save", "sav")).toBeGreaterThan(score("Save All Scripts", "sav"));
    });

    test("spaces in the query are ignored", () => {
        expect(positions("Open File", "o f")).toEqual([0, 5]);
    });
});

describe("toMatchSegments", () => {
    test("no positions leaves the string in one unmatched run", () => {
        expect(toMatchSegments("Open File", [])).toEqual([{text: "Open File", matched: false}]);
    });

    test("an empty string produces no runs", () => {
        expect(toMatchSegments("", [])).toEqual([]);
    });

    test("adjacent positions collapse into one matched run", () => {
        expect(toMatchSegments("Open File", [0, 1])).toEqual([
            {text: "Op", matched: true},
            {text: "en File", matched: false},
        ]);
    });

    test("separated positions produce alternating runs", () => {
        expect(toMatchSegments("Open File", [0, 5])).toEqual([
            {text: "O", matched: true},
            {text: "pen ", matched: false},
            {text: "F", matched: true},
            {text: "ile", matched: false},
        ]);
    });

    test("a match at the end closes the string", () => {
        expect(toMatchSegments("Run", [2])).toEqual([
            {text: "Ru", matched: false},
            {text: "n", matched: true},
        ]);
    });

    test("the runs reassemble into the original string", () => {
        const text = "Switch to Last Active Script";
        const match = fuzzyMatch(text, "slas")!;

        expect(toMatchSegments(text, match.positions).map(s => s.text).join("")).toBe(text);
    });
});
