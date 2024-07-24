import * as Excel from "exceljs";

export interface IExcelExportOptions {
    freezeHeaders?: boolean
    sheetPerOutputItem?: boolean,
    includeCode?: boolean
    includeNonTabularData?: boolean
    headerBackgroundColor?: string,
    headerForegroundColor?: string,
}

interface IWritePosition {
    row: number;
    column: number;
}

export class ExcelService {
    public static export(elements: Element[], options?: IExcelExportOptions) {
        options ??= {};

        const workbook = new Excel.Workbook();
        let worksheet: Excel.Worksheet | null = null;

        for (let iElement = 0; iElement < elements.length; iElement++) {
            const element = document.createRange().createContextualFragment(elements[iElement].outerHTML).firstElementChild;
            if (!element) continue;
            element.querySelectorAll(".null, .table-info-header").forEach(x => x.remove());

            const position: IWritePosition = {
                row: 1,
                column: 1,
            };

            if (!worksheet || options.sheetPerOutputItem) {
                worksheet = workbook.addWorksheet(`Sheet ${workbook.worksheets.length + 1}`);
            } else if (iElement > 0) {
                position.row = worksheet.rowCount + 2;
            }


            if (element.tagName === "TABLE") {
                const table = element as HTMLTableElement;

                if (options.freezeHeaders && !worksheet.views.length) {
                    worksheet.views = [{
                        state: 'frozen',
                        ySplit: (table.tHead && table.tHead.rows.length > 0) ? 1 : 0,
                        xSplit: table.tBodies.length && table.tBodies[0].rows[0].firstElementChild?.tagName === "TH" ? 1 : 0
                    }];
                }

                this.writeTable(worksheet, table, position, options);
            } else {
                const cell = worksheet.getCell(position.row, position.column);
                cell.value = this.getTextContent(element);
            }
        }

        return workbook;
    }

    private static writeTable(worksheet: Excel.Worksheet, table: HTMLTableElement, position: IWritePosition, options: IExcelExportOptions) {
        if (!table || (!table.tHead?.rows.length && (!table.tBodies.length || !table.tBodies[0].rows.length))) return;

        const widths = this.getColumnWidths(table);
        if (table.tHead && table.tHead.rows.length) {
            const wsRow = worksheet.getRow(position.row);
            let columnNumber = position.column;

            for (let iTableCell = 0; iTableCell < table.tHead.rows[0].cells.length; iTableCell++) {

                const cell = table.tHead.rows[0].cells[iTableCell];
                const wsCell = wsRow.getCell(columnNumber);
                wsCell.value = this.getTextContent(cell);

                const column = widths[iTableCell];
                if (column.width > 1) {
                    worksheet.mergeCells(position.row, columnNumber, position.row, columnNumber + column.width - 1);
                    columnNumber = columnNumber + column.width;
                } else {
                    columnNumber++;
                }

                this.styleCell(cell, wsCell, options);
            }

            position.row++;
        }

        if (table.tBodies.length && table.tBodies[0].rows.length) {
            const rows = table.tBodies[0].rows;

            for (let iTableRow = 0; iTableRow < rows.length; iTableRow++) {
                const row = rows[iTableRow];
                const cells = row?.cells;
                if (!row || !cells || !cells.length) continue;

                const wsRow = worksheet.getRow(position.row);
                let columnNumber = position.column;

                try {
                    for (let iTableCell = 0; iTableCell < cells.length; iTableCell++) {
                        const cell = cells[iTableCell];
                        if (!cell) continue;

                        const column = widths[iTableCell];

                        const wsCell = wsRow.getCell(columnNumber);

                        try {
                            if (cell.firstElementChild?.tagName !== "TABLE") {
                                wsCell.value = this.getTextContent(cell);
                            } else {
                                const innerTable = cell.firstElementChild as HTMLTableElement;

                                const tmp: IWritePosition = {
                                    row: position.row,
                                    column: columnNumber,
                                };

                                this.writeTable(worksheet, innerTable, tmp, options);

                                // Update current row position
                                // -1 to off put the extra row increment that happens inside the writeTable method
                                position.row = tmp.row - 1;
                            }

                            this.styleCell(cell, wsCell, options);
                        } finally {
                            if (column && column.width > 1) {
                                columnNumber = columnNumber + column.width;
                            } else {
                                columnNumber++;
                            }
                        }
                    }
                } finally {
                    position.row++;
                }
            }
        }
    }

    private static getColumnWidths(table: HTMLTableElement): { iColumn: number, width: number }[] {
        if (!table || !table.tHead || !table.tHead.rows.length || !table.tBodies) return [];

        return Array.from(table.tHead.rows[0].cells)
            .map((cell, index) => {
                const iColumn = index;
                let width = 1;

                if (table.tBodies.length && table.tBodies[0].rows.length) {
                    for (let iRow = 0; iRow < table.tBodies[0].rows.length; iRow++) {
                        const row = table.tBodies[0].rows[iRow];
                        const cells = row?.cells;
                        if (!row || !cells || cells.length < (iColumn + 1)) continue;
                        const cell = cells[iColumn];
                        const innerTable = cell.firstElementChild;
                        if (innerTable?.tagName === "TABLE") {
                            const innerWidth = this.getTableWidth(innerTable as HTMLTableElement);
                            if (innerWidth > width) width = innerWidth;
                        }
                    }
                }

                return {
                    iColumn: iColumn,
                    width: width
                }
            });
    }

    private static getTableWidth(table: HTMLTableElement): number {
        if (!table || !table.tHead || !table.tBodies) return 0;
        let total = table.tHead && table.tHead.rows && table.tHead.rows.length > 0 ? table.tHead.rows[0].childElementCount : 0;

        if (table.tBodies.length && table.tBodies[0].rows.length) {
            let max = 0;

            for (let iRow = 0; iRow < table.tBodies[0].rows.length; iRow++) {
                const row = table.tBodies[0].rows[iRow];
                const cells = row?.cells;
                if (!row || !cells || !cells.length) continue;

                for (let iCell = 0; iCell < row.cells.length; iCell++) {
                    const cell = row.cells[iCell];
                    const innerTable = cell.firstElementChild;
                    if (innerTable?.tagName === "TABLE") {
                        const innerWidth = this.getTableWidth(innerTable as HTMLTableElement) - 1;
                        if (innerWidth > max) max = innerWidth;
                    }
                }
            }

            total += max;
        }

        return total;
    }

    private static styleCell(tableCell: HTMLTableCellElement, worksheetCell: Excel.Cell, options: IExcelExportOptions) {
        if (tableCell.tagName === "TH") {
            if (options.headerForegroundColor) {
                worksheetCell.style.font = {
                    name: "Calibri",
                    bold: true,
                    color: {argb: "ff" + (options.headerForegroundColor.replace("#", ""))}
                };
            }

            if (options.headerBackgroundColor) {
                worksheetCell.fill = {
                    type: "pattern",
                    pattern: "lightGray",
                    bgColor: {argb: "ff" + (options.headerBackgroundColor.replace("#", ""))}
                }
            }
        }
    }

    private static getTextContent(element: Element) {
        element.querySelectorAll("br").forEach(x => x.replaceWith("\n"));
        return element.textContent;
    }
}
