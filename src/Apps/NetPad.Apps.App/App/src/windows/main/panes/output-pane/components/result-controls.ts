import {WithDisposables} from "@common";
import {ResizableTable} from "@application/tables/resizable-table";

/** What a table cell's rendered text represents. */
type CellKind = "numeric" | "boolean-true" | "boolean-false" | "null" | "other";

export class ResultControls extends WithDisposables {
    // O2Html renders a collection's header as "<type> (N items)", or "(First N items)"
    // when the collection was truncated.
    private static readonly itemCountPattern = /\s*\((?:First\s)?\d+\sitems\)\s*$/;
    private static readonly numericPattern = /^[-+]?\d+(?:\.\d+)?$/;

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

                const caret = document.createElement("i");
                collapseTarget.prepend(caret);
                caret.classList.add("caret-up-icon", "me-2");

                this.extractItemCount(collapseTarget);
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
     * Splits the item count out of a table's header text so the header can render it as a chip
     * beside the type name instead of as parenthesised text.
     */
    private extractItemCount(header: Element) {
        const textNode = Array.from(header.childNodes)
            .reverse()
            .find(n => n.nodeType === Node.TEXT_NODE && !!n.textContent?.trim()) as Text | undefined;

        const text = textNode?.textContent;
        if (!textNode || !text) return;

        const match = ResultControls.itemCountPattern.exec(text);
        if (!match) return;

        const count = document.createElement("span");
        count.classList.add("item-count");
        count.textContent = match[0].trim().slice(1, -1);

        textNode.textContent = text.slice(0, match.index);
        textNode.after(count);
    }

    /**
     * Tags value cells with what they hold, so numbers can be set in mono and aligned as a column
     * and booleans can carry the true/false colors. The serializer emits every value as text, so
     * the only signal available here is the rendered string.
     *
     * Numbers align right only in tables whose rows are items of a collection — in a table whose
     * rows are one object's properties there is no column to align them against.
     */
    private classifyValues(table: HTMLTableElement) {
        if (table.tBodies.length === 0) return;

        const isCollection = !!table.querySelector(":scope > thead > tr.table-data-header");
        if (isCollection) table.classList.add("columnar");

        // A column is numeric only if every value in it is; nulls and blanks abstain.
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

                const kind = ResultControls.getCellKind(cell);

                if (kind === "boolean-true" || kind === "boolean-false") {
                    cell.classList.add(kind);
                }

                if (kind === "null" || (kind === "other" && !cell.textContent?.trim())) continue;

                if (!isCollection) {
                    if (kind === "numeric") cell.classList.add("numeric");
                } else {
                    numericColumns.set(index, kind === "numeric" && numericColumns.get(index) !== false);
                }
            }
        }

        if (!isCollection) return;

        for (const [index, numeric] of numericColumns) {
            if (!numeric) continue;

            for (const row of Array.from(table.querySelectorAll(":scope > tbody > tr"))) {
                row.children[index]?.classList.add("numeric");
            }

            table.querySelector(":scope > thead > tr.table-data-header")
                ?.children[index]?.classList.add("numeric");
        }
    }

    private static getCellKind(cell: HTMLTableCellElement): CellKind {
        if (cell.childElementCount > 0) {
            return cell.children.length === 1 && cell.children[0].classList.contains("null")
                ? "null"
                : "other";
        }

        const text = (cell.textContent ?? "").trim();

        if (text === "True") return "boolean-true";
        if (text === "False") return "boolean-false";

        return ResultControls.numericPattern.test(text) ? "numeric" : "other";
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
