import {ResizableTable} from "@application";
import {WithDisposables} from "@common";

export class ResultControls extends WithDisposables {
    constructor(private readonly resultsElement: HTMLElement) {
        super();
    }

    public bind(content: DocumentFragment) {

        for (const table of Array.from(content.querySelectorAll("table"))) {

            table.classList.add("table", "table-sm", "table-bordered");

            // Collapse/Expand functionality
            const collapseTarget = this.getTableCollapseTarget(table);
            if (collapseTarget) {
                collapseTarget.classList.add("collapse-actionable");
                const clickHandler = (e) => {
                    if (e.target === collapseTarget) this.toggle(table);
                };
                collapseTarget.addEventListener("click", clickHandler);

                this.addDisposable(() => {
                    collapseTarget?.removeEventListener("click", clickHandler);
                });

                const caret = document.createElement("i");
                collapseTarget.prepend(caret);
                caret.classList.add("caret-up-icon", "me-2");
            }

            const resizableTable = new ResizableTable(table);
            resizableTable.init();
            this.addDisposable(resizableTable);

            if (table.tBodies.length > 0) {
                const cells = Array.from(table.querySelectorAll(":scope > tbody > tr > td")) as HTMLTableCellElement[];
                for (const cell of cells) {
                    // If cell only contains text, and that text is relatively long
                    if (cell.childElementCount === 0 && cell.innerHTML.length > 200) {
                        cell.style.maxWidth = "30vw";
                    }
                }
            }
        }
    }

    public expand(table: HTMLTableElement) {
        table.classList.remove("collapsed");
        const caretIcon = this.getTableCollapseTarget(table)?.querySelector(".caret-down-icon");
        if (caretIcon) {
            caretIcon.classList.remove("caret-down-icon");
            caretIcon.classList.add("caret-up-icon");
        }
    }

    public collapse(table: HTMLTableElement) {
        table.classList.add("collapsed");
        const caretIcon = this.getTableCollapseTarget(table)?.querySelector(".caret-up-icon");
        if (caretIcon) {
            caretIcon.classList.remove("caret-up-icon");
            caretIcon.classList.add("caret-down-icon");
        }
    }

    public toggle(table: HTMLTableElement) {
        if (table.classList.contains("collapsed"))
            this.expand(table);
        else
            this.collapse(table);
    }

    private getTableCollapseTarget(table: HTMLTableElement): Element | null {
        let collapseTarget = table.querySelector(":scope > thead > tr > th.table-info-header");
        if (!collapseTarget)
            collapseTarget = table.querySelector(":scope > thead > tr > th");

        return collapseTarget;
    }

    private expandAllTables() {
        this.querySelectorAll("table").forEach(t => this.expand(t as HTMLTableElement));
    }

    private collapseAllTables() {
        this.querySelectorAll("table").forEach(t => this.collapse(t as HTMLTableElement));
    }

    querySelectorAll(selectors: string) {
        return Array.from(this.resultsElement.querySelectorAll(selectors))
    }
}
