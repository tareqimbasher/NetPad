import {WithDisposables} from "@common";
import {ResizableTable} from "@application/tables/resizable-table";
import {IconName, createIconElement} from "@application/ui/np-icon/icons";

export class ResultControls extends WithDisposables {
    constructor(private readonly resultsElement: HTMLElement) {
        super();
    }

    public bind(content: DocumentFragment) {

        for (const titledGroup of Array.from(content.querySelectorAll(".group.titled"))) {
            const title = titledGroup.querySelector(".title");
            if (!title) continue;

            const clickHandler = (e: Event) => {
                const selection = document.getSelection();
                if (selection && selection.toString() && (e.target as Element).contains(selection.anchorNode)) return;

                if (titledGroup.classList.contains("collapsed")) titledGroup.classList.remove("collapsed");
                else titledGroup.classList.add("collapsed");
            };

            title.addEventListener("click", clickHandler);
            this.addDisposable(() => title.removeEventListener("click", clickHandler));
        }

        for (const table of Array.from(content.querySelectorAll("table"))) {

            table.classList.add("table", "table-sm", "table-bordered");

            table.parentElement?.classList.add("has-table");

            // Collapse/Expand functionality
            const collapseTarget = this.getTableCollapseTarget(table);
            if (collapseTarget) {
                collapseTarget.classList.add("collapse-actionable");
                const clickHandler = (e: Event) => {
                    const selection = document.getSelection();
                    if (selection && selection.toString() && (e.target as Element).contains(selection.anchorNode)) return;

                    if (e.target === collapseTarget) this.toggle(table);
                };
                collapseTarget.addEventListener("click", clickHandler);

                this.addDisposable(() => {
                    collapseTarget?.removeEventListener("click", clickHandler);
                });

                const caret = createIconElement("chevron-down");
                if (caret) {
                    caret.classList.add("dump-caret", "me-2");
                    collapseTarget.prepend(caret);
                }
            }

            const resizableTable = new ResizableTable(table);
            resizableTable.init();
            this.addDisposable(resizableTable);

            this.classifyValues(table);
        }

        for (const group of Array.from(content.querySelectorAll(".group[data-destruct]:not([data-destruct=''])"))) {
            const val = group.getAttribute("data-destruct");
            if (!val) {
                continue;
            }

            const milliseconds = Number(val);
            if (isNaN(milliseconds)) {
                continue;
            }

            setTimeout(() => group.remove(), milliseconds);
        }
    }

    /**
     * Decides which columns of a table hold nothing but numbers, and marks every cell in them so
     * they can align right.
     */
    private classifyValues(table: HTMLTableElement) {
        if (table.tBodies.length === 0) return;

        const isCollection = !!table.querySelector(":scope > thead > tr.table-data-header");
        if (isCollection) {
            table.classList.add("columnar");
        }

        // A column is numeric only if every value in it is.
        const numericColumns = new Map<number, boolean>();

        for (const row of Array.from(table.querySelectorAll(":scope > tbody > tr"))) {
            const cells = Array.from(row.children) as HTMLTableCellElement[];

            for (let index = 0; index < cells.length; index++) {
                const cell = cells[index];
                if (cell.tagName !== "TD") continue;

                // If cell only contains text, and that text is relatively long
                if (cell.childElementCount === 0 && cell.innerHTML.length > 200) {
                    cell.style.maxWidth = "30vw";
                }

                // If a cell contains null or is empty, don't make it affect the result of
                // the column being a numeric column otherwise a single nullable int value
                // for example will result in the whole column being considered not-numeric
                if (!isCollection || ResultControls.containsNullOrIsEmpty(cell)) continue;

                const stillNumeric = numericColumns.get(index) ?? true;
                numericColumns.set(index, stillNumeric && cell.classList.contains("numeric"));
            }
        }

        if (!isCollection) return;

        for (const [index, numeric] of numericColumns) {
            if (!numeric) continue;

            for (const row of Array.from(table.querySelectorAll(":scope > tbody > tr"))) {
                row.children[index]?.classList.add("numeric-column");
            }

            table.querySelector(":scope > thead > tr.table-data-header")
                ?.children[index]?.classList.add("numeric-column");
        }
    }

    private static containsNullOrIsEmpty(cell: HTMLTableCellElement): boolean {
        if (cell.childElementCount === 0) {
            return !cell.textContent?.trim();
        }

        return cell.children.length === 1 && cell.children[0].classList.contains("null");
    }

    public expand(table: HTMLTableElement) {
        table.classList.remove("collapsed");
        ResultControls.setCaret(this.getTableCollapseTarget(table), "chevron-down");
    }

    public collapse(table: HTMLTableElement) {
        table.classList.add("collapsed");
        ResultControls.setCaret(this.getTableCollapseTarget(table), "chevron-right");
    }

    public toggle(table: HTMLTableElement) {
        if (table.classList.contains("collapsed"))
            this.expand(table);
        else
            this.collapse(table);
    }

    private static setCaret(collapseTarget: Element | null | undefined, icon: IconName) {
        const caret = collapseTarget?.querySelector(".dump-caret");
        if (!caret) return;

        const replacement = createIconElement(icon);
        if (!replacement) return;

        replacement.classList.add(...caret.classList);
        caret.replaceWith(replacement);
    }

    private getTableCollapseTarget(table: HTMLTableElement): Element | null {
        let collapseTarget = table.querySelector(":scope > thead > tr.table-info-header > th");
        if (!collapseTarget)
            collapseTarget = table.querySelector(":scope > thead > tr > th");

        return collapseTarget;
    }

    public expandAll(level?: number) {
        if (!level) {
            this.querySelectorAll("table").forEach(t => this.expand(t as HTMLTableElement));
            this.querySelectorAll(".group.titled").forEach(t => t.classList.remove("collapsed"));
            return;
        }

        let selector = "";

        for (let iLevel = level; iLevel > 0; iLevel--) {
            selector += (!selector ? "" : ", ") + ".group > table";

            for (let iLevel2 = 1; iLevel2 < iLevel; iLevel2++) {
                selector += " > tbody > tr > td > table";
            }
        }

        this.resultsElement.querySelectorAll(selector).forEach(v => this.expand(v as HTMLTableElement));
    }

    public collapseAll(level?: number, root?: Element | DocumentFragment) {
        if (!root) root = this.resultsElement;

        if (!level) {
            root.querySelectorAll("table").forEach(t => this.collapse(t as HTMLTableElement));
            root.querySelectorAll(".group.titled").forEach(t => t.classList.add("collapsed"));
            return;
        }

        let selector = ".group > table";
        for (let iLevel = 1; iLevel <= level; iLevel++) {
            selector += " > tbody > tr > td > table";
        }

        selector += `, ${selector} table`;
        root.querySelectorAll(selector).forEach(v => this.collapse(v as HTMLTableElement));
    }

    private querySelectorAll(selectors: string) {
        return Array.from(this.resultsElement.querySelectorAll(selectors))
    }
}
