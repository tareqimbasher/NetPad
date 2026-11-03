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

            const table = ResultControls.titledTable(titledGroup);

            const clickHandler = (e: Event) => {
                const selection = document.getSelection();
                if (selection && selection.toString() && (e.target as Element).contains(selection.anchorNode)) return;

                if (table) this.toggle(table);
                else if (titledGroup.classList.contains("collapsed")) titledGroup.classList.remove("collapsed");
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

                    if ((e.target as Element).closest(".column-resizer, a")) return;

                    this.toggle(table);
                };
                collapseTarget.addEventListener("click", clickHandler);

                this.addDisposable(() => {
                    collapseTarget?.removeEventListener("click", clickHandler);
                });

                this.placeCaret(table);
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

                // Null and empty cells don't count, or a single null would make an `int?` column non-numeric.
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
        this.placeCaret(table);
    }

    public collapse(table: HTMLTableElement) {
        table.classList.add("collapsed");
        this.placeCaret(table);
    }

    public toggle(table: HTMLTableElement) {
        if (table.classList.contains("collapsed"))
            this.expand(table);
        else
            this.collapse(table);
    }

    private placeCaret(table: HTMLTableElement) {
        const titledGroup = table.parentElement?.closest(".group.titled");
        const host = titledGroup && ResultControls.titledTable(titledGroup) === table
            ? titledGroup.querySelector(":scope > .title")
            : this.getTableCollapseTarget(table);
        if (!host) return;

        const icon: IconName = table.classList.contains("collapsed") ? "chevron-right" : "chevron-down";
        const caret = createIconElement(icon);
        if (!caret) return;

        caret.classList.add("dump-caret");

        const existing = host.querySelector(":scope > .dump-caret");
        if (existing) existing.replaceWith(caret);
        else host.prepend(caret);
    }

    private static titledTable(titledGroup: Element): HTMLTableElement | null {
        return titledGroup.querySelector(":scope > table");
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
            root.querySelectorAll(".group.titled").forEach(t => {
                if (!ResultControls.titledTable(t)) t.classList.add("collapsed");
            });
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
