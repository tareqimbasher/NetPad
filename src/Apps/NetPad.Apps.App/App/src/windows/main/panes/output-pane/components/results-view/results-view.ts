import {System, Util} from "@common";
import {ChannelInfo, IIpcGateway, ScriptStatus} from "@application";
import {ExcelExportDialog} from "../excel-export/excel-export-dialog";
import {ExcelService, IExcelExportOptions} from "../excel-export/excel-service";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {AppTheme} from "@application/themes/app-theme";
import {CustomCss} from "@application/themes/custom-css";
import {OutputViewBase} from "../output-view-base";

export class ResultsView extends OutputViewBase {
    private txtUserInput: HTMLInputElement;

    constructor(@IIpcGateway private readonly ipcGateway: IIpcGateway,
                private readonly dialogUtil: DialogUtil,
    ) {
        super();
    }

    public override attached() {
        super.attached();

        const userInputKeyHandler = async (ev: KeyboardEvent) => {
            if (ev.key !== "Enter") {
                return;
            }

            const inputRequest = this.model.inputRequest;
            if (!inputRequest) {
                return;
            }

            await this.ipcGateway.send(
                new ChannelInfo("Respond"),
                inputRequest.commandId,
                inputRequest.userInput);

            this.model.inputRequest = null;
        };

        this.txtUserInput.addEventListener("keydown", userInputKeyHandler);
        this.addDisposable(() => this.txtUserInput.removeEventListener("keydown", userInputKeyHandler));
    }

    private async exportOutputToExcel() {
        if (!this.model) {
            return;
        }

        const dumpContainer = this.model.resultsDumpContainer;

        const groups = Array.from(dumpContainer.element.querySelectorAll(".group"));

        if (!groups.length) {
            alert("There is no output to export.");
            return;
        }

        const result = await this.dialogUtil.open(ExcelExportDialog);

        if (result?.status !== "ok") {
            return;
        }

        const exportOptions = result.value as IExcelExportOptions;

        const elementsToExport: Element[] = [];
        for (const group of Array.from(dumpContainer.element.querySelectorAll(".group"))) {
            const table = group.querySelector(":scope > table");
            if (table) {
                elementsToExport.push(table);
                continue;
            }

            if (!exportOptions.includeNonTabularData) continue;

            const title = group.querySelector(":scope > .title");
            if (title && group.childElementCount > 1 && group.lastElementChild) {
                elementsToExport.push(group.lastElementChild)
            } else if (!title) {
                elementsToExport.push(group);
            }
        }

        const workbook = ExcelService.export(elementsToExport, exportOptions);

        if (exportOptions.includeCode) {
            const worksheet = workbook.addWorksheet("Code");
            worksheet.getRow(1).height = 2000;
            worksheet.getColumn(1).width = 300;
            const firstCell = worksheet.getCell(1, 1);
            firstCell.alignment = {vertical: "top", horizontal: "left"};
            firstCell.value = this.model.environment.script.code;
        }

        const buffer = (await workbook.xlsx.writeBuffer()) as unknown as Buffer;

        System.downloadFile(
            `${this.model.environment.script.name}_${Util.formatDate(new Date(), "yyyy-MM-dd_HH-mm-ss")}.xlsx`,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            buffer
        );
    }

    private async exportOutputToHtml() {
        if (!this.model) {
            return;
        }

        const name = `${this.model.environment.script.name}_${Util.formatDate(new Date(), "yyyy-MM-dd_HH-mm-ss")}`

        const metas = [...new Set<string>(
            Array.from(document.head.querySelectorAll("meta"))
                .map(s => s.outerHTML)
        )].join("\n");

        const styles = [...new Set(
            Array.from(document.head.querySelectorAll("style"))
                .map(s => s.outerHTML)
                .filter(x => x.indexOf("output-pane") >= 0)
        )].join("\n");

        const themeClasses = Array.from(document.documentElement.classList)
            .filter(c => c.startsWith(AppTheme.cssClassPrefix));

        // Custom user styles should be emitted last so it overrides app styles.
        const customCss = CustomCss.element?.outerHTML ?? "";

        const bodyContents = document.createRange().createContextualFragment(this.dumpContainerWrapper.outerHTML);
        bodyContents.querySelectorAll("np-icon, .np-icon").forEach(x => x.remove());

        const html = `<!DOCTYPE html>
<html lang="en" class="${themeClasses.join(" ")}">
<head>
<title>${name}</title>
${metas}
<style>
${ResultsView.collectThemeCss(themeClasses)}
body {
    margin: 0;
    background: var(--bg0);
    color: var(--text);
    font-family: ${getComputedStyle(document.documentElement).getPropertyValue("--font-sans")};
}
output-pane { display: block; }
</style>
${styles}
${customCss}
</head>
<body>
<output-pane>${bodyContents.firstElementChild?.outerHTML}</output-pane>
</body></html>`;

        System.downloadTextAsFile(`${name}.html`, "text/html", html);
    }

    /**
     * Collects the CSS styles from the active theme. The theme's tokens can be spread over more
     * than one class on the root (the palette's, plus the background modifier), so a rule travels
     * with the export when the export has every class it is scoped to.
     */
    private static collectThemeCss(themeClasses: string[]): string {
        if (themeClasses.length === 0) return "";

        const scopedToTheme = (selector: string) => {
            const scope = selector.split(" ")[0];

            return /^(\.[\w-]+)+$/.test(scope)
                && scope.split(".").filter(Boolean).every(c => themeClasses.includes(c));
        };

        const rules: string[] = [];

        for (const sheet of Array.from(document.styleSheets)) {
            let sheetRules: CSSRuleList;
            try {
                sheetRules = sheet.cssRules;
            } catch {
                continue;
            }

            for (const rule of Array.from(sheetRules)) {
                if (!(rule instanceof CSSStyleRule)) continue;

                const applies = rule.selectorText
                    .split(",")
                    .map(s => s.trim())
                    .some(scopedToTheme);

                if (applies) rules.push(rule.cssText);
            }
        }

        return rules.join("\n");
    }
}
