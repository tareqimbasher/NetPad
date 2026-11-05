import {ILogger} from "aurelia";
import {FindTextBox} from "@application/find-text-box/find-text-box";

// The barrel loads components, which need their templates, and these tests do not render one.
jest.mock("@application", () => ({
    ...jest.requireActual("@application/keybindings/key-combo"),
    ...jest.requireActual("@application/view-model-base"),
}));
jest.mock("@application/find-text-box/find-text-box.html", () => ({name: "find-text-box", template: ""}));

describe("FindTextBox", () => {
    let output: HTMLElement;
    let box: FindTextBox;

    const textAndCount = () => {
        const internals = box as unknown as {
            searchText: string,
            current?: {searchResults?: {matches: unknown[]}}
        };
        return {
            text: internals.searchText,
            count: internals.current?.searchResults?.matches.length ?? 0,
        };
    };

    // The watch on the input's text does not run outside a rendered component, so typing is the
    // assignment plus the search the watch would start.
    const type = (text: string) => {
        (box as unknown as {searchText: string}).searchText = text;
        box.search(text);
    };

    const pressCtrlF = () =>
        output.dispatchEvent(new KeyboardEvent("keydown", {key: "f", ctrlKey: true, bubbles: true}));

    const addLine = (text: string) => {
        const line = document.createElement("div");
        line.className = "group text";
        line.textContent = text;
        output.append(line);
    };

    beforeEach(() => {
        jest.useFakeTimers();
        Element.prototype.scrollIntoView = jest.fn();

        const logger = {debug: jest.fn(), error: jest.fn()} as unknown as ILogger;
        (logger as unknown as {scopeTo: () => ILogger}).scopeTo = () => logger;

        box = new FindTextBox(document.createElement("div"), logger);
        (box as unknown as {txtSearch: HTMLInputElement}).txtSearch = document.createElement("input");

        output = document.createElement("div");
        document.body.append(output);
        addLine("SELECT 1");
        addLine("SELECT 2");

        box.registerSearchableElement(output, ".group.text");
        box.setCurrent(output);
    });

    afterEach(() => {
        output.remove();
        jest.useRealTimers();
    });

    test("shows the highlights again when it reopens", () => {
        pressCtrlF();
        type("SELECT");
        box.close();
        expect(output.classList).toContain("hide-text-search-results");

        pressCtrlF();

        expect(output.classList).not.toContain("hide-text-search-results");
        expect(output.querySelectorAll(".text-search-result")).toHaveLength(2);
        expect(textAndCount()).toEqual({text: "SELECT", count: 2});
    });

    test("counts what the output shows when it reopens", () => {
        pressCtrlF();
        type("SELECT");
        box.close();

        addLine("SELECT 3");
        pressCtrlF();

        expect(output.querySelectorAll(".text-search-result")).toHaveLength(3);
        expect(textAndCount().count).toBe(3);
    });

    test("keeps its place when Ctrl+F is pressed while it is open", () => {
        pressCtrlF();
        type("SELECT");
        box.goToNextResult();
        const current = () => (box as unknown as {current: {searchResults: {currentResultNumber: number}}})
            .current.searchResults.currentResultNumber;
        expect(current()).toBe(2);

        const search = jest.spyOn(box, "search");
        pressCtrlF();

        expect(current()).toBe(2);
        expect(search).not.toHaveBeenCalled();
    });

    test("takes the selected text when it opens", () => {
        pressCtrlF();
        type("SELECT");
        box.close();

        addLine("WHERE");
        const selection = window.getSelection()!;
        selection.selectAllChildren(output.lastElementChild!);

        const search = jest.spyOn(box, "search");
        pressCtrlF();

        expect(textAndCount().text).toBe("WHERE");
        expect(search).not.toHaveBeenCalledWith("SELECT");
        selection.removeAllRanges();
    });
});
