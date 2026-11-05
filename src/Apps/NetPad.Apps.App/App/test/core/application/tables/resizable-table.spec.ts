import {ResizableTable} from "../../../../src/core/@application/tables/resizable-table";

describe("ResizableTable", () => {
    let table: HTMLTableElement;
    let resizable: ResizableTable | undefined;

    const headings = () => Array.from(table.querySelectorAll("thead th")) as HTMLElement[];
    const resizersIn = (heading: Element) => heading.querySelectorAll(":scope > .column-resizer");

    beforeEach(() => {
        document.body.innerHTML =
            `<table><thead><tr><th>Name</th><th>Age</th><th>City</th></tr></thead><tbody><tr><td>Ada</td><td>36</td><td>London</td></tr></tbody></table>`;
        table = document.querySelector("table")!;
    });

    afterEach(() => resizable?.dispose());

    it("gives each heading of the last header row a resizer", () => {
        resizable = new ResizableTable(table);
        resizable.init();

        expect(headings().map(h => resizersIn(h).length)).toEqual([1, 1, 1]);
    });

    it("gives the skipped heading no resizer", () => {
        resizable = new ResizableTable(table, headings()[1]);
        resizable.init();

        expect(headings().map(h => resizersIn(h).length)).toEqual([1, 0, 1]);
    });

    it("prevents the default of a mousedown on a resizer", () => {
        resizable = new ResizableTable(table);
        resizable.init();
        const event = new MouseEvent("mousedown", {bubbles: true, cancelable: true});

        resizersIn(headings()[0])[0].dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
    });
});
