import {
    comparePaletteMatches,
    matchPaletteItem,
    orderGroupsByBestMatch,
    PaletteMatch
} from "@application/command-palette/palette-matching";

function text(segments: { text: string }[] | undefined): string {
    return (segments ?? []).map(s => s.text).join("");
}

function hits(segments: { text: string; matched: boolean }[] | undefined): string {
    return (segments ?? []).filter(s => s.matched).map(s => s.text).join("");
}

const script = {title: "Repo Stats", detail: "Scripts/Samples"};

describe("matchPaletteItem", () => {
    test("a title hit marks the title and leaves the detail plain", () => {
        const match = matchPaletteItem(script, "repo")!;

        expect(match.onTitle).toBe(true);
        expect(hits(match.titleSegments)).toBe("Repo");
        expect(hits(match.detailSegments)).toBe("");
        expect(text(match.detailSegments)).toBe("Scripts/Samples");
    });

    test("a title miss falls back to the detail, so a folder name finds what is in it", () => {
        const match = matchPaletteItem(script, "samples")!;

        expect(match.onTitle).toBe(false);
        expect(hits(match.detailSegments)).toBe("Samples");
        expect(hits(match.titleSegments)).toBe("");
        expect(text(match.titleSegments)).toBe("Repo Stats");
    });

    test("a query neither the title nor the detail carries does not match", () => {
        expect(matchPaletteItem(script, "zzz")).toBeUndefined();
    });

    test("a row without a detail is matched on its title alone", () => {
        const command = {title: "Save All"};

        expect(matchPaletteItem(command, "save")?.onTitle).toBe(true);
        expect(matchPaletteItem(command, "scripts")).toBeUndefined();
        expect(matchPaletteItem(command, "save")?.detailSegments).toBeUndefined();
    });

    test("an empty query matches everything on the title", () => {
        const match = matchPaletteItem(script, "")!;

        expect(match.onTitle).toBe(true);
        expect(match.score).toBe(0);
        expect(text(match.titleSegments)).toBe("Repo Stats");
    });
});

describe("comparePaletteMatches", () => {
    const titleHit = {score: 1, onTitle: true} as PaletteMatch;
    const betterTitleHit = {score: 30, onTitle: true} as PaletteMatch;
    const detailHit = {score: 50, onTitle: false} as PaletteMatch;

    test("a name hit outranks a location hit even when the location scores higher", () => {
        expect(comparePaletteMatches(titleHit, detailHit)).toBeLessThan(0);
    });

    test("within the same kind of hit, the better score wins", () => {
        expect(comparePaletteMatches(betterTitleHit, titleHit)).toBeLessThan(0);
    });
});

describe("orderGroupsByBestMatch", () => {
    const group = (label: string, best: Partial<PaletteMatch>) =>
        ({label, best: best as PaletteMatch});

    test("the section holding the better hit leads", () => {
        const groups = [
            group("Application", {score: 5, onTitle: true}),
            group("Editor", {score: 40, onTitle: true}),
        ];

        expect(orderGroupsByBestMatch(groups, g => g.best).map(g => g.label))
            .toEqual(["Editor", "Application"]);
    });

    test("a name hit keeps its section ahead of a section found only by location", () => {
        const groups = [
            group("Open", {score: 1, onTitle: true}),
            group("Library", {score: 90, onTitle: false}),
        ];

        expect(orderGroupsByBestMatch(groups, g => g.best).map(g => g.label))
            .toEqual(["Open", "Library"]);
    });

    test("sections whose best hits tie keep source order", () => {
        const groups = [
            group("Open", {score: 20, onTitle: true}),
            group("Recent", {score: 20, onTitle: true}),
            group("Library", {score: 20, onTitle: true}),
        ];

        expect(orderGroupsByBestMatch(groups, g => g.best).map(g => g.label))
            .toEqual(["Open", "Recent", "Library"]);
    });

    test("ordering does not mutate the array it was given", () => {
        const groups = [
            group("Application", {score: 5, onTitle: true}),
            group("Editor", {score: 40, onTitle: true}),
        ];

        orderGroupsByBestMatch(groups, g => g.best);

        expect(groups.map(g => g.label)).toEqual(["Application", "Editor"]);
    });
});
