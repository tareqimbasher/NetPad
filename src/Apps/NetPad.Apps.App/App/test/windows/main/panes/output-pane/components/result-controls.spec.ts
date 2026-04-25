import {ResultControls} from "../../../../../../src/windows/main/panes/output-pane/components/result-controls";

describe("ResultControls", () => {
    test("adds a bar graph toggle for numeric columns", () => {
        const {content, table} = createFragment();

        const controls = new ResultControls(document.createElement("div"));
        controls.bind(content);

        const header = table.tHead?.rows[1].cells[1];
        expect(header?.querySelector(".bar-graph-toggle")).not.toBeNull();
        controls.dispose();
    });

    test("toggles bar graphs for numeric cells", () => {
        const {content, table} = createFragment();

        const controls = new ResultControls(document.createElement("div"));
        controls.bind(content);

        const button = table.tHead?.rows[1].cells[1].querySelector(".bar-graph-toggle") as HTMLElement;
        button.click();

        const numericCells = Array.from(table.tBodies[0].rows).map(row => row.cells[1]);
        expect(numericCells.every(cell => cell.classList.contains("bar-graph-cell"))).toBe(true);
        expect(numericCells[0].querySelector(".bar-graph-fill")).not.toBeNull();

        button.click();

        expect(numericCells.every(cell => !cell.classList.contains("bar-graph-cell"))).toBe(true);
        expect(numericCells[0].querySelector(".bar-graph-fill")).toBeNull();
        controls.dispose();
    });
});

function createFragment() {
    const template = document.createElement("template");
    template.innerHTML = `
        <table>
            <thead>
                <tr class="table-info-header">
                    <th colspan="2">Sample (2 items)</th>
                </tr>
                <tr class="table-data-header">
                    <th>Name</th>
                    <th data-bar-graph="true" title="System.Int32">Age</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Alice</td>
                    <td data-bar-graph-value="10">10</td>
                </tr>
                <tr>
                    <td>Bob</td>
                    <td data-bar-graph-value="20">20</td>
                </tr>
            </tbody>
        </table>
    `;

    return {
        content: template.content,
        table: template.content.querySelector("table") as HTMLTableElement
    };
}
