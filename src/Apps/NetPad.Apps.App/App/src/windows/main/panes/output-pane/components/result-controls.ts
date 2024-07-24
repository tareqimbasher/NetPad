import {WithDisposables} from "@common";
import {ResizableTable} from "@application/tables/resizable-table";

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
