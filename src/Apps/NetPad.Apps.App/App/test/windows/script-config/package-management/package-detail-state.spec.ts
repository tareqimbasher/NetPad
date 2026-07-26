import {
    computeDetailState,
    DetailStateInput,
} from "../../../../src/windows/script-config/package-management/package-detail-state";

/**
 * Characterization tests: they pin the package browser's derived-state rules (the A25g action
 * truth table and the collapsed-version pinning) that were previously only reachable by driving
 * the live app. They describe current behavior; the structural refactor must keep them green.
 */

function input(overrides: Partial<DetailStateInput> = {}): DetailStateInput {
    return {
        package: {packageId: "Foo", title: "Foo", version: "2.0.0", latestAvailableVersion: "2.0.0", dependencies: []},
        pickedVersion: undefined,
        fetchedVersions: undefined,
        expanded: false,
        filter: "",
        referencedVersion: undefined,
        cachedVersions: [],
        ...overrides,
    };
}

describe("computeDetailState — primary action (A25g truth table)", () => {
    it("unreferenced offers 'Reference <target>' at the latest version by default", () => {
        const state = computeDetailState(input({referencedVersion: undefined}));
        expect(state.primaryAction).toEqual({label: "Reference 2.0.0", version: "2.0.0"});
    });

    it("unreferenced with an older version picked targets the pick", () => {
        const state = computeDetailState(input({pickedVersion: "1.0.0"}));
        expect(state.primaryAction).toEqual({label: "Reference 1.0.0", version: "1.0.0"});
    });

    it("referenced with a different version picked offers to switch to the pick", () => {
        const state = computeDetailState(input({referencedVersion: "1.0.0", pickedVersion: "0.9.0"}));
        expect(state.primaryAction).toEqual({label: "Reference 0.9.0", version: "0.9.0"});
    });

    it("referenced below latest (no pick) offers 'Update to <latest>'", () => {
        const state = computeDetailState(input({referencedVersion: "1.0.0"}));
        expect(state.primaryAction).toEqual({label: "Update to 2.0.0", version: "2.0.0"});
    });

    it("referenced at latest offers no primary action", () => {
        const state = computeDetailState(input({referencedVersion: "2.0.0"}));
        expect(state.primaryAction).toBeNull();
    });

    it("referenced with the referenced version re-picked still resolves to Update, not a switch", () => {
        const state = computeDetailState(input({referencedVersion: "1.0.0", pickedVersion: "1.0.0"}));
        expect(state.primaryAction).toEqual({label: "Update to 2.0.0", version: "2.0.0"});
    });

    it("offers no primary action when there is no target version at all", () => {
        const state = computeDetailState(input({
            package: {packageId: "Foo", title: "Foo", dependencies: []},
            fetchedVersions: undefined,
        }));
        expect(state.primaryAction).toBeNull();
    });
});

describe("computeDetailState — danger and download flags", () => {
    it("a referenced package can be removed and never deleted-from-cache or downloaded", () => {
        const state = computeDetailState(input({referencedVersion: "2.0.0", cachedVersions: ["2.0.0"]}));
        expect(state.canRemove).toBe(true);
        expect(state.canDeleteFromCache).toBe(false);
        expect(state.canDownloadOnly).toBe(false);
    });

    it("an unreferenced package whose target is cached can be deleted-from-cache, not downloaded", () => {
        const state = computeDetailState(input({cachedVersions: ["2.0.0"]}));
        expect(state.canRemove).toBe(false);
        expect(state.canDeleteFromCache).toBe(true);
        expect(state.canDownloadOnly).toBe(false);
    });

    it("an unreferenced package whose target is not cached can be downloaded, not deleted", () => {
        const state = computeDetailState(input({cachedVersions: []}));
        expect(state.canRemove).toBe(false);
        expect(state.canDeleteFromCache).toBe(false);
        expect(state.canDownloadOnly).toBe(true);
    });

    it("download/delete track the picked version, not the latest", () => {
        // Latest (2.0.0) is not cached but the picked older version is → delete, not download.
        const state = computeDetailState(input({pickedVersion: "1.0.0", cachedVersions: ["1.0.0"]}));
        expect(state.canDeleteFromCache).toBe(true);
        expect(state.canDownloadOnly).toBe(false);
    });
});

describe("computeDetailState — context line and id prefix", () => {
    it("omits the id prefix when the id equals the title", () => {
        const state = computeDetailState(input({
            package: {packageId: "Newtonsoft.Json", title: "Newtonsoft.Json", version: "13.0.3", dependencies: []},
            referencedVersion: "13.0.3",
        }));
        expect(state.contextLine).toBe("referenced 13.0.3");
    });

    it("prefixes the id when it differs from the title", () => {
        const state = computeDetailState(input({
            package: {packageId: "Newtonsoft.Json", title: "Json.NET", version: "13.0.3", dependencies: []},
            referencedVersion: "13.0.3",
        }));
        expect(state.contextLine).toBe("Newtonsoft.Json · referenced 13.0.3");
    });

    it("reports a cached target when unreferenced and cached", () => {
        const state = computeDetailState(input({cachedVersions: ["2.0.0"]}));
        expect(state.contextLine).toBe("cached 2.0.0");
    });

    it("reports 'not cached' when unreferenced and not cached", () => {
        const state = computeDetailState(input({cachedVersions: []}));
        expect(state.contextLine).toBe("not cached");
    });
});

describe("computeDetailState — collapsed version pinning", () => {
    const ten = ["1.9", "1.8", "1.7", "1.6", "1.5", "1.4", "1.3", "1.2", "1.1", "1.0"];

    it("shows only the most recent handful by default", () => {
        const state = computeDetailState(input({fetchedVersions: ten}));
        expect(state.displayedVersions.map(v => v.version)).toEqual(["1.9", "1.8", "1.7", "1.6", "1.5"]);
        expect(state.hasMoreVersions).toBe(true);
    });

    it("pins a referenced version that falls past the head slice, in version order", () => {
        const state = computeDetailState(input({fetchedVersions: ten, referencedVersion: "1.1"}));
        expect(state.displayedVersions.map(v => v.version)).toEqual(["1.9", "1.8", "1.7", "1.6", "1.5", "1.1"]);
        expect(state.displayedVersions.find(v => v.version === "1.1")!.referenced).toBe(true);
    });

    it("pins a cached version that falls past the head slice", () => {
        const state = computeDetailState(input({fetchedVersions: ten, cachedVersions: ["1.2"]}));
        expect(state.displayedVersions.map(v => v.version)).toContain("1.2");
        expect(state.displayedVersions.find(v => v.version === "1.2")!.cached).toBe(true);
    });

    it("pins the explicitly picked version past the head slice", () => {
        const state = computeDetailState(input({fetchedVersions: ten, pickedVersion: "1.0"}));
        expect(state.displayedVersions.map(v => v.version)).toContain("1.0");
    });

    it("appends a cached pre-release absent from a stable-only fetch", () => {
        const state = computeDetailState(input({
            fetchedVersions: ["2.0.0", "1.0.0"],
            cachedVersions: ["2.0.0-rc"],
        }));
        expect(state.displayedVersions.map(v => v.version)).toEqual(["2.0.0", "1.0.0", "2.0.0-rc"]);
        expect(state.displayedVersions.find(v => v.version === "2.0.0-rc")!.cached).toBe(true);
    });

    it("reports no more versions when the whole fetched list already fits", () => {
        const state = computeDetailState(input({fetchedVersions: ["3.0", "2.0", "1.0"]}));
        expect(state.displayedVersions.map(v => v.version)).toEqual(["3.0", "2.0", "1.0"]);
        expect(state.hasMoreVersions).toBe(false);
    });

    it("marks latest/referenced/cached chips on each row", () => {
        const state = computeDetailState(input({
            fetchedVersions: ["2.0.0", "1.0.0"],
            referencedVersion: "1.0.0",
            cachedVersions: ["1.0.0"],
        }));
        const latest = state.displayedVersions.find(v => v.version === "2.0.0")!;
        const older = state.displayedVersions.find(v => v.version === "1.0.0")!;
        expect(latest).toMatchObject({latest: true, referenced: false, cached: false});
        expect(older).toMatchObject({latest: false, referenced: true, cached: true});
    });
});

describe("computeDetailState — expanded version list and filter", () => {
    const ten = ["1.9", "1.8", "1.7", "1.6", "1.5", "1.4", "1.3", "1.2", "1.1", "1.0"];

    it("shows every fetched version when expanded with no filter", () => {
        const state = computeDetailState(input({fetchedVersions: ten, expanded: true}));
        expect(state.displayedVersions).toHaveLength(ten.length);
        expect(state.hasMoreVersions).toBe(false);
    });

    it("filters the expanded list case-insensitively by substring", () => {
        const state = computeDetailState(input({fetchedVersions: ["1.10", "1.1", "2.0"], expanded: true, filter: " 1.1 "}));
        expect(state.displayedVersions.map(v => v.version)).toEqual(["1.10", "1.1"]);
        expect(state.hasMoreVersions).toBe(false);
    });
});

describe("computeDetailState — latest resolution and dependencies label", () => {
    it("falls back to the first fetched version when the package carries no version fields", () => {
        const state = computeDetailState(input({
            package: {packageId: "Foo", title: "Foo", dependencies: []},
            fetchedVersions: ["5.0", "4.0"],
        }));
        expect(state.primaryAction).toEqual({label: "Reference 5.0", version: "5.0"});
    });

    it("labels the dependencies section with the TFM group count, pluralized", () => {
        expect(computeDetailState(input({package: {packageId: "Foo", title: "Foo", dependencies: []}})).dependenciesLabel)
            .toBe("Dependencies");
        expect(computeDetailState(input({package: {packageId: "Foo", title: "Foo", dependencies: [{}]}})).dependenciesLabel)
            .toBe("Dependencies · 1 TFM");
        expect(computeDetailState(input({package: {packageId: "Foo", title: "Foo", dependencies: [{}, {}, {}]}})).dependenciesLabel)
            .toBe("Dependencies · 3 TFMs");
    });

    it("returns an empty version list while versions are still loading", () => {
        const state = computeDetailState(input({fetchedVersions: undefined}));
        expect(state.displayedVersions).toEqual([]);
        expect(state.hasMoreVersions).toBe(false);
    });
});
