import {WithDisposables} from "@common";

export class ResizableTable extends WithDisposables {
    constructor(public table: HTMLTableElement) {
        super();
    }

    public init() {
        const thead = this.table.tHead;
        if (thead == null || thead.rows.length === 0)
            return;

        const headings = Array.from(thead.rows[thead.rows.length - 1].querySelectorAll("th"));
        for (const heading of headings) {

            const resizer = document.createElement("div");
            resizer.classList.add("column-resizer");
            resizer.style.zIndex = "1";
            resizer.style.top = "0";
            resizer.style.right = "-4px";
            resizer.style.width = "6px";
            resizer.style.bottom = "0";
            resizer.style.position = "absolute";
            resizer.style.cursor = "col-resize";
            resizer.style.userSelect = "none";
            resizer.style.backgroundColor = "transparent";

            heading.style.position = "relative";
            heading.appendChild(resizer);
            this.setListeners(this.table, resizer);
        }
    }

    private setListeners(table: HTMLTableElement, resizer: HTMLElement) {
        let pageX: number | undefined,
            curCol: HTMLElement | null | undefined,
            curColWidth: number | undefined,
            tableWidth: number;

        const setResizerHeight = () => {
            const tableRect = this.table.getBoundingClientRect();
            const tableBottomY = tableRect.height + tableRect.y;
            const resizerHeight = tableBottomY - resizer.getBoundingClientRect().y;

            resizer.style.height = `${resizerHeight}px`;
        };

        const tableMouseOverHandler = () => {
            // We set the height of the resizer here because table height might change
            setResizerHeight();
        };

        const mouseDownHandler = (e: MouseEvent) => {
            tableWidth = table.offsetWidth;
            curCol = (e.target as HTMLElement).parentElement;
            pageX = e.pageX;

            if (!curCol) {
                return;
            }

            const padding = this.paddingDiff(curCol);

            curColWidth = curCol.offsetWidth - padding;
        };

        const mouseOverHandler = (e: MouseEvent) => {
            (e.target as HTMLElement).style.borderLeft = '2px solid dodgerblue';
            // We set the height of the resizer here because table height might change
            setResizerHeight();
        };

        const mouseOutHandler = (e: MouseEvent) => {
            (e.target as HTMLElement).style.borderLeft = '';
        };

        const mouseMoveHandler = (e: MouseEvent) => {
            if (curCol && pageX !== undefined && curColWidth !== undefined) {
                const diffX = e.pageX - pageX;
                curCol.style.width = (curColWidth + diffX) + 'px';
                table.style.width = tableWidth + diffX + "px"
            }
        };

        const mouseUpHandler = () => {
            curCol = undefined;
            pageX = undefined;
            curColWidth = undefined
        };

        table.addEventListener("mouseover", tableMouseOverHandler);
        resizer.addEventListener('mousedown', mouseDownHandler);
        resizer.addEventListener('mouseover', mouseOverHandler);
        resizer.addEventListener('mouseout', mouseOutHandler);
        document.addEventListener('mouseup', mouseUpHandler);
        document.addEventListener('mousemove', mouseMoveHandler);

        this.addDisposable(() => {
            table.removeEventListener("mouseover", tableMouseOverHandler);
            resizer.removeEventListener('mousedown', mouseDownHandler);
            resizer.removeEventListener('mouseover', mouseOverHandler);
            resizer.removeEventListener('mouseout', mouseOutHandler);
            document.removeEventListener('mouseup', mouseUpHandler);
            document.removeEventListener('mousemove', mouseMoveHandler);
        });
    }

    private paddingDiff(col: Element) {
        if (this.getStyleVal(col, 'box-sizing') == 'border-box') {
            return 0;
        }

        const padLeft = this.getStyleVal(col, 'padding-left');
        const padRight = this.getStyleVal(col, 'padding-right');
        return (parseInt(padLeft) + parseInt(padRight));

    }

    private getStyleVal(el: Element, css: string) {
        return (window.getComputedStyle(el, null).getPropertyValue(css))
    }
}
