import {ResultControls} from "../../../../../../src/windows/main/panes/output-pane/components/result-controls";
import {createIconElement} from "../../../../../../src/core/@application/ui/np-icon/icons";

const typeLineHtml = (type: string) => `${type}<span class="item-count">1 items</span><span class="dump-elapsed">at 1 ms</span>`;
const tableHtml = (type: string, rows: string) =>
    `<table><thead><tr class="table-info-header"><th colspan="2">${typeLineHtml(type)}</th></tr></thead><tbody>${rows}</tbody></table>`;

const titledTable = `<div class="group titled"><h6 class="title">People</h6>${tableHtml("Person[]", "<tr><td>Ada</td></tr>")}</div>`;
const untitledTable = `<div class="group">${tableHtml("Person[]", "<tr><td>Ada</td></tr>")}</div>`;
const titledScalar = `<div class="group titled text"><h6 class="title">Answer</h6><span class="text">42</span><br></div>`;
const titledNested = `<div class="group titled"><h6 class="title">Nested</h6>${tableHtml("Team",
    `<tr><th>Lead</th><td>${tableHtml("Lead", `<tr><th>Tags</th><td>${tableHtml("Tag[]", "<tr><td>k</td></tr>")}</td></tr>`)}</td></tr>`)}</div>`;

describe("ResultControls", () => {
    let root: HTMLElement;
    let controls: ResultControls;

    const render = (html: string) => {
        const template = document.createElement("template");
        template.innerHTML = html;
        controls.bind(template.content);
        root.append(template.content);
    };

    const groups = () => Array.from(root.children) as HTMLElement[];
    const tableOf = (group: Element) => group.querySelector(":scope > table") as HTMLTableElement;
    const typeLine = (table: Element) => table.querySelector(":scope > thead > tr.table-info-header > th") as HTMLElement;
    const title = (group: Element) => group.querySelector(":scope > .title") as HTMLElement;
    const typeOf = (table: Element) => typeLine(table).querySelector(":scope > .dump-type-line > .dump-type") as HTMLElement;
    const caretsIn = (element: Element) => element.querySelectorAll(":scope > .dump-caret");

    const pointsRight = (caret: Element) => caret.innerHTML === createIconElement("chevron-right")!.innerHTML;
    const pointsDown = (caret: Element) => caret.innerHTML === createIconElement("chevron-down")!.innerHTML;

    beforeEach(() => {
        root = document.createElement("div");
        document.body.append(root);
        controls = new ResultControls(root);
    });

    afterEach(() => {
        controls.dispose();
        root.remove();
    });

    describe("a titled table", () => {
        let group: HTMLElement;
        let table: HTMLTableElement;

        beforeEach(() => {
            render(titledTable);
            group = groups()[0];
            table = tableOf(group);
        });

        test("puts the caret on the title line and none on the type line", () => {
            expect(caretsIn(title(group))).toHaveLength(1);
            expect(typeLine(table).querySelectorAll(".dump-caret")).toHaveLength(0);
            expect(pointsDown(caretsIn(title(group))[0])).toBe(true);
        });

        test("collapses its table, not itself, when the title is clicked", () => {
            title(group).click();

            expect(table.classList.contains("collapsed")).toBe(true);
            expect(group.classList.contains("collapsed")).toBe(false);
            expect(pointsRight(caretsIn(title(group))[0])).toBe(true);

            title(group).click();

            expect(table.classList.contains("collapsed")).toBe(false);
            expect(pointsDown(caretsIn(title(group))[0])).toBe(true);
        });

        test("collapses its table when the type line is clicked, with the caret staying on the title", () => {
            typeLine(table).click();

            expect(table.classList.contains("collapsed")).toBe(true);
            expect(group.classList.contains("collapsed")).toBe(false);
            expect(typeLine(table).querySelectorAll(".dump-caret")).toHaveLength(0);
            expect(pointsRight(caretsIn(title(group))[0])).toBe(true);
        });

        test("collapse all collapses its table and leaves the group open", () => {
            controls.collapseAll();

            expect(table.classList.contains("collapsed")).toBe(true);
            expect(group.classList.contains("collapsed")).toBe(false);
            expect(pointsRight(caretsIn(title(group))[0])).toBe(true);

            controls.expandAll();

            expect(table.classList.contains("collapsed")).toBe(false);
            expect(pointsDown(caretsIn(title(group))[0])).toBe(true);
        });

        test("keeps one caret when its saved HTML is rendered again", () => {
            title(group).click();
            const saved = root.innerHTML;
            root.innerHTML = "";

            render(saved);

            const again = groups()[0];
            expect(caretsIn(title(again))).toHaveLength(1);
            expect(pointsRight(caretsIn(title(again))[0])).toBe(true);
        });
    });

    describe("a click anywhere on the type line", () => {
        const clickOn = (element: Element) => element.dispatchEvent(new MouseEvent("click", {bubbles: true}));

        test.each([".dump-caret", ".item-count", ".dump-elapsed"])("on an untitled table's %s toggles it", part => {
            render(untitledTable);
            const table = tableOf(groups()[0]);

            clickOn(typeLine(table).querySelector(part)!);
            expect(table.classList.contains("collapsed")).toBe(true);

            clickOn(typeLine(table).querySelector(part)!);
            expect(table.classList.contains("collapsed")).toBe(false);
        });

        test.each([".item-count", ".dump-elapsed"])("on a titled table's %s toggles it", part => {
            render(titledTable);
            const table = tableOf(groups()[0]);

            clickOn(typeLine(table).querySelector(part)!);

            expect(table.classList.contains("collapsed")).toBe(true);
        });
    });

    test("the type line has no column resizer", () => {
        render(untitledTable);
        const table = tableOf(groups()[0]);

        expect(typeLine(table).querySelector(".column-resizer")).toBeNull();
    });

    test("an untitled table keeps its caret on the type line", () => {
        render(untitledTable);
        const table = tableOf(groups()[0]);

        typeLine(table).click();

        expect(table.classList.contains("collapsed")).toBe(true);
        expect(caretsIn(typeOf(table))).toHaveLength(1);
        expect(pointsRight(caretsIn(typeOf(table))[0])).toBe(true);
    });

    describe("the type line", () => {
        const partsOf = (element: Element) => Array.from(element.childNodes)
            .map(node => node instanceof Element ? `.${node.classList[node.classList.length - 1]}` : node.textContent);

        test("groups the caret, the type and the count, with the timing after them", () => {
            render(untitledTable);
            const table = tableOf(groups()[0]);

            expect(partsOf(typeLine(table))).toEqual([".dump-type-line"]);
            expect(partsOf(typeLine(table).firstElementChild!)).toEqual([".dump-type", ".dump-elapsed"]);
            expect(partsOf(typeOf(table))).toEqual([".dump-caret", "Person[]", ".item-count"]);
        });

        test("keeps its text", () => {
            render(untitledTable);

            expect(typeLine(tableOf(groups()[0])).textContent).toBe("Person[]1 itemsat 1 ms");
        });

        test("is laid out on a titled table too, with no caret", () => {
            render(titledTable);

            expect(partsOf(typeOf(tableOf(groups()[0])))).toEqual(["Person[]", ".item-count"]);
        });

        test("keeps one row and one caret when its saved HTML is rendered again", () => {
            render(untitledTable);
            typeLine(tableOf(groups()[0])).click();
            const saved = root.innerHTML;
            root.innerHTML = "";

            render(saved);

            const table = tableOf(groups()[0]);
            expect(typeLine(table).querySelectorAll(".dump-type-line")).toHaveLength(1);
            expect(caretsIn(typeOf(table))).toHaveLength(1);
            expect(pointsRight(caretsIn(typeOf(table))[0])).toBe(true);
        });
    });

    test("a titled scalar collapses as a whole", () => {
        render(titledScalar);
        const group = groups()[0];

        title(group).click();
        expect(group.classList.contains("collapsed")).toBe(true);

        controls.expandAll();
        expect(group.classList.contains("collapsed")).toBe(false);

        controls.collapseAll();
        expect(group.classList.contains("collapsed")).toBe(true);
    });

    describe("collapse and expand to a level", () => {
        let group: HTMLElement;
        let levels: HTMLTableElement[];

        beforeEach(() => {
            render(titledNested);
            group = groups()[0];
            const outer = tableOf(group);
            const lead = outer.querySelector(":scope > tbody > tr > td > table") as HTMLTableElement;
            const tags = lead.querySelector(":scope > tbody > tr > td > table") as HTMLTableElement;
            levels = [outer, lead, tags];
        });

        const collapsed = () => levels.map(t => t.classList.contains("collapsed"));

        test.each([
            [1, [false, true, true]],
            [2, [false, false, true]],
        ])("collapse to level %d keeps the levels above it open", (level, expected) => {
            controls.collapseAll(level);

            expect(collapsed()).toEqual(expected);
            expect(group.classList.contains("collapsed")).toBe(false);
        });

        test.each([
            [1, [false, true, true]],
            [2, [false, false, true]],
            [3, [false, false, false]],
        ])("expand to level %d opens the levels down to it", (level, expected) => {
            controls.collapseAll();

            controls.expandAll(level);

            expect(collapsed()).toEqual(expected);
            expect(group.classList.contains("collapsed")).toBe(false);
            expect(pointsDown(caretsIn(title(group))[0])).toBe(true);
        });
    });
});
